"use client";

import { useState, useEffect, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Image from "next/image";
import { Eye, EyeOff, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { useAuthStore } from "@/lib/store";
import { authApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";

export default function LoginPage() {
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnUrl = searchParams.get("returnUrl") || "/";
  const { login, isAuthenticated } = useAuthStore();
  const { t } = useTranslation();
  const [hydrated, setHydrated] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    setHydrated(true);
  }, []);

  useEffect(() => {
    if (hydrated && isAuthenticated) {
      router.push(returnUrl);
    }
  }, [hydrated, isAuthenticated, router, returnUrl]);

  if (!hydrated || isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-[var(--color-brand-green)]/5 via-background to-[var(--color-brand-orange)]/5">
        <div className="flex flex-col items-center gap-3">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
          <p className="text-sm text-muted-foreground">{t("common.loading")}</p>
        </div>
      </div>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);

    try {
      const tokenResponse = await authApi.login(email, password);

      if (tokenResponse.access_token) {
        const payload = authApi.parseToken(tokenResponse.access_token);

        if (payload) {
          const role = Array.isArray(payload.role) ? payload.role[0] : (payload.role || "admin");
          const user = {
            id: payload.sub,
            username: payload.preferred_username || payload.given_name,
            email: payload.email,
            role,
          };

          login(user, tokenResponse.access_token);
          router.push(returnUrl);
        } else {
          setError(t("auth.tokenParseFailed"));
        }
      } else {
        setError(t("auth.invalidResponse"));
      }
    } catch (err: unknown) {
      console.error("Login error:", err);

      if (err && typeof err === "object" && "response" in err) {
        const axiosError = err as { response?: { data?: { error_description?: string; error?: string }; status?: number } };
        if (axiosError.response?.data?.error_description) {
          setError(axiosError.response.data.error_description);
        } else if (axiosError.response?.data?.error) {
          setError(axiosError.response.data.error);
        } else if (axiosError.response?.status === 400) {
          setError(t("auth.invalidCredentials"));
        } else {
          setError(t("auth.authFailed"));
        }
      } else {
        setError(t("auth.connectionFailed"));
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-[var(--color-brand-green)]/5 via-background to-[var(--color-brand-orange)]/5 p-4">
      <Card className="w-full max-w-[420px] shadow-lg">
        <CardContent className="p-8">
          {/* Brand */}
          <div className="mb-8 flex flex-col items-center text-center">
            <Image src="/logo.png" alt="K-Charge" width={474} height={317} className="mb-2 h-32 w-auto" priority />
            <p className="mt-1 text-sm text-muted-foreground">
              {t("auth.networkManagement")}
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div role="alert" className="flex items-center gap-2 rounded-lg bg-destructive/10 p-3 text-sm text-destructive">
                <AlertCircle className="h-4 w-4 flex-shrink-0" />
                <span>{error}</span>
              </div>
            )}

            <div className="space-y-1.5">
              <label htmlFor="email" className="text-sm font-medium">
                {t("auth.username")}
              </label>
              <input
                id="email"
                type="text"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder={t("auth.enterUsername")}
                required
                autoComplete="username"
                className="h-10 w-full rounded-lg border bg-background px-3 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary"
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="password" className="text-sm font-medium">
                {t("auth.password")}
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder={t("auth.enterPassword")}
                  required
                  autoComplete="current-password"
                  className="h-10 w-full rounded-lg border bg-background px-3 pr-10 text-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </div>

            <Button type="submit" className="w-full h-10" disabled={isLoading}>
              {isLoading ? (
                <span className="flex items-center gap-2">
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  {t("auth.signingIn")}
                </span>
              ) : (
                t("auth.signIn")
              )}
            </Button>
          </form>

          {/* Demo credentials */}
          <div className="mt-6 rounded-lg border border-dashed p-4 text-sm">
            <p className="font-medium text-foreground mb-2">{t("auth.demoCredentials")}</p>
            <div className="space-y-1 text-muted-foreground text-xs">
              <p><span className="font-medium text-foreground">{t("auth.admin")}:</span> admin / Admin@123</p>
              <p><span className="font-medium text-foreground">{t("auth.operator")}:</span> operator / Admin@123</p>
              <p><span className="font-medium text-foreground">{t("auth.viewer")}:</span> viewer / Admin@123</p>
            </div>
          </div>

          {/* Footer */}
          <p className="mt-6 text-center text-xs text-muted-foreground">
            {t("common.poweredBy")}
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
