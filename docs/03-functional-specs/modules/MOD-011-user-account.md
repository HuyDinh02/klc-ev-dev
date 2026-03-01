# MOD-011: User Account & Profile

> Status: APPROVED | Priority: Phase 1 | Last Updated: 2026-03-01

## 1. Overview
Manages EV driver accounts in the mobile app: registration, authentication, profile management, and security settings. Extends ABP IdentityUser.

## 2. Actors
| Actor | Role |
|-------|------|
| EV Driver | Register, login, manage profile and security |

## 3. Functional Requirements
| ID | Requirement | Priority |
|----|------------|----------|
| FR-011-01 | User registration with email/phone verification | Must |
| FR-011-02 | Secure login with password, optional social login | Must |
| FR-011-03 | Update personal information (name, phone, email) | Must |
| FR-011-04 | Change password with current password verification | Must |
| FR-011-05 | View and manage account settings | Must |
| FR-011-06 | View charging history summary on profile | Should |
| FR-011-07 | Account deactivation (soft delete) | Should |

## 4. Business Rules
| ID | Rule |
|----|------|
| BR-011-01 | Email must be unique across all users |
| BR-011-02 | Phone number must be unique across all users |
| BR-011-03 | Password must meet minimum security requirements (8+ chars, mixed case, digit) |
| BR-011-04 | User with active session cannot deactivate account |
| BR-011-05 | Registration mandatory before using charging features |

## 5. Data Model
### AppUser (extends ABP IdentityUser)
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| PhoneNumber | string(20) | Yes | User phone number |
| FullName | string(200) | Yes | Display name |
| AvatarUrl | string(500) | No | Profile picture URL |
| IsVerified | bool | Yes | Email/phone verified |
| PreferredLanguage | string(5) | Yes | vi or en |

## 6. API Endpoints (Driver BFF)
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | /api/v1/auth/register | Register new user | Public |
| POST | /api/v1/auth/login | Login | Public |
| POST | /api/v1/auth/refresh-token | Refresh JWT token | Driver |
| GET | /api/v1/profile | Get current user profile | Driver |
| PUT | /api/v1/profile | Update profile | Driver |
| POST | /api/v1/profile/change-password | Change password | Driver |

## 7. Error Handling
| Code | Message | HTTP Status |
|------|---------|-------------|
| MOD_011_001 | Email already registered | 409 |
| MOD_011_002 | Phone already registered | 409 |
| MOD_011_003 | Invalid credentials | 401 |
| MOD_011_004 | Current password incorrect | 400 |

## 8. Testing Scenarios
| ID | Scenario | Expected Result |
|----|----------|----------------|
| TC-011-01 | Register with valid data | Account created, verification sent |
| TC-011-02 | Register with existing email | 409 error |
| TC-011-03 | Login with valid credentials | JWT token returned |
| TC-011-04 | Update profile | Info updated successfully |
| TC-011-05 | Change password | Password updated, old sessions invalidated |
