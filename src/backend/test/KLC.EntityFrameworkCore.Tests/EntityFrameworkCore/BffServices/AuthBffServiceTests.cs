using System;
using System.Threading.Tasks;
using KLC.Auditing;
using KLC.Configuration;
using KLC.Driver.Services;
using KLC.EntityFrameworkCore;
using KLC.Notifications;
using KLC.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;
using Volo.Abp.Identity;
using Xunit;

namespace KLC.BffServices;

[Collection(KLCTestConsts.CollectionDefinitionName)]
public class AuthBffServiceTests : KLCEntityFrameworkCoreTestBase
{
    private readonly KLCDbContext _dbContext;
    private readonly IdentityUserManager _userManager;
    private readonly IDatabase _redisDb;
    private readonly ISmsService _smsService;
    private readonly IAuditEventLogger _auditLogger;
    private readonly AuthBffService _service;

    public AuthBffServiceTests()
    {
        _dbContext = GetRequiredService<KLCDbContext>();
        _userManager = GetRequiredService<IdentityUserManager>();

        _redisDb = Substitute.For<IDatabase>();
        _redisDb.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>()).Returns(true);
        _redisDb.StringGetAsync(Arg.Any<RedisKey>()).Returns(RedisValue.Null);

        var redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_redisDb);

        _smsService = Substitute.For<ISmsService>();
        _auditLogger = Substitute.For<IAuditEventLogger>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>())
            .Build();

        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "ThisIsATestSecretKeyThatIsLongEnoughForHS256!",
            Issuer = "KLC.Test",
            Audience = "KLC.Test",
            ExpiryMinutes = 60
        });

        var logger = Substitute.For<ILogger<AuthBffService>>();

        _service = new AuthBffService(
            _dbContext,
            _userManager,
            redis,
            configuration,
            jwtSettings,
            logger,
            _smsService,
            _auditLogger);
    }

    [Fact]
    public async Task Login_Should_Succeed_With_Correct_Password()
    {
        var identityUserId = Guid.NewGuid();
        var phone = "0901000001";
        var password = "Test@123456";

        // Create IdentityUser via UserManager
        await WithUnitOfWorkAsync(async () =>
        {
            var identityUser = new IdentityUser(identityUserId, phone, $"{phone}@klc.local");
            var createResult = await _userManager.CreateAsync(identityUser, password);
            createResult.Succeeded.ShouldBeTrue();

            var appUser = new AppUser(Guid.NewGuid(), identityUserId, "Test Login User", phone);
            appUser.VerifyPhone();
            await _dbContext.AppUsers.AddAsync(appUser);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.LoginAsync(new LoginRequest
            {
                PhoneNumber = phone,
                Password = password
            });

            result.Success.ShouldBeTrue();
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNullOrEmpty();
            result.User.ShouldNotBeNull();
            result.User!.FullName.ShouldBe("Test Login User");
        });
    }

    [Fact]
    public async Task Login_Should_Fail_With_Wrong_Password()
    {
        var identityUserId = Guid.NewGuid();
        var phone = "0901000002";
        var password = "Correct@123456";

        await WithUnitOfWorkAsync(async () =>
        {
            var identityUser = new IdentityUser(identityUserId, phone, $"{phone}@klc.local");
            await _userManager.CreateAsync(identityUser, password);

            var appUser = new AppUser(Guid.NewGuid(), identityUserId, "Wrong Pass User", phone);
            await _dbContext.AppUsers.AddAsync(appUser);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.LoginAsync(new LoginRequest
            {
                PhoneNumber = phone,
                Password = "WrongPassword@999"
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldBe(KLCDomainErrorCodes.Auth.InvalidCredentials);
        });
    }

    [Fact]
    public async Task Login_Should_Fail_When_Account_Inactive()
    {
        var identityUserId = Guid.NewGuid();
        var phone = "0901000003";
        var password = "Test@123456";

        await WithUnitOfWorkAsync(async () =>
        {
            var identityUser = new IdentityUser(identityUserId, phone, $"{phone}@klc.local");
            await _userManager.CreateAsync(identityUser, password);

            var appUser = new AppUser(Guid.NewGuid(), identityUserId, "Inactive User", phone);
            appUser.Deactivate();
            await _dbContext.AppUsers.AddAsync(appUser);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.LoginAsync(new LoginRequest
            {
                PhoneNumber = phone,
                Password = password
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldBe(KLCDomainErrorCodes.Auth.AccountSuspended);
        });
    }

    [Fact]
    public async Task Login_Should_Fail_When_User_Not_Found()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var result = await _service.LoginAsync(new LoginRequest
            {
                PhoneNumber = "0999999999",
                Password = "AnyPassword@123"
            });

            result.Success.ShouldBeFalse();
            result.Error.ShouldBe(KLCDomainErrorCodes.Auth.InvalidCredentials);
        });
    }

    [Fact]
    public async Task ForgotPassword_Should_Store_OTP_In_Redis()
    {
        var identityUserId = Guid.NewGuid();
        var phone = "0901000004";

        await WithUnitOfWorkAsync(async () =>
        {
            var appUser = new AppUser(Guid.NewGuid(), identityUserId, "Forgot Pass User", phone);
            await _dbContext.AppUsers.AddAsync(appUser);
            await _dbContext.SaveChangesAsync();
        });

        await WithUnitOfWorkAsync(async () =>
        {
            await _service.ForgotPasswordAsync(new ForgotPasswordRequest
            {
                PhoneNumber = phone
            });
        });

        // Verify OTP was stored in Redis (StoreOtp calls StringSetAsync with key "otp:reset:{phone}")
        await _redisDb.Received(1).StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());

        // Verify SMS was sent
        await _smsService.Received(1).SendAsync(
            phone,
            Arg.Is<string>(msg => msg.Contains("reset code")));
    }

    [Fact]
    public async Task ForgotPassword_Should_Not_Reveal_NonExistent_Phone()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            // Should complete silently even if phone doesn't exist
            await _service.ForgotPasswordAsync(new ForgotPasswordRequest
            {
                PhoneNumber = "0999888777"
            });
        });

        // No OTP stored, no SMS sent
        await _redisDb.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
        await _smsService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ResetPassword_Should_Fail_With_Invalid_OTP()
    {
        var phone = "0901000005";

        // Redis returns null for OTP lookup (no stored OTP)
        _redisDb.StringGetAsync(Arg.Is<RedisKey>(k => k.ToString() == $"otp:reset:{phone}"))
            .Returns(RedisValue.Null);

        await WithUnitOfWorkAsync(async () =>
        {
            var ex = await Should.ThrowAsync<Volo.Abp.BusinessException>(async () =>
            {
                await _service.ResetPasswordAsync(new ResetPasswordRequest
                {
                    PhoneNumber = phone,
                    Otp = "123456",
                    NewPassword = "NewPassword@123"
                });
            });

            ex.Code.ShouldBe(KLCDomainErrorCodes.Auth.InvalidOtp);
        });
    }

    [Fact]
    public async Task ResetPassword_Should_Fail_With_Wrong_OTP()
    {
        var phone = "0901000006";

        // Redis returns a different OTP than what the user provides
        _redisDb.StringGetAsync(Arg.Is<RedisKey>(k => k.ToString() == $"otp:reset:{phone}"))
            .Returns((RedisValue)"654321");

        await WithUnitOfWorkAsync(async () =>
        {
            var ex = await Should.ThrowAsync<Volo.Abp.BusinessException>(async () =>
            {
                await _service.ResetPasswordAsync(new ResetPasswordRequest
                {
                    PhoneNumber = phone,
                    Otp = "123456", // Wrong OTP
                    NewPassword = "NewPassword@123"
                });
            });

            ex.Code.ShouldBe(KLCDomainErrorCodes.Auth.InvalidOtp);
        });
    }
}
