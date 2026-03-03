"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Zap, Eye, EyeOff, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { useAuthStore } from "@/lib/store";
import { authApi } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const { login, isAuthenticated } = useAuthStore();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      router.push("/");
    }
  }, [isAuthenticated, router]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);

    try {
      // Call the real OpenIddict token endpoint
      const tokenResponse = await authApi.login(email, password);

      if (tokenResponse.access_token) {
        // Parse JWT to extract user info
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
          router.push("/");
        } else {
          setError("Failed to parse authentication token");
        }
      } else {
        setError("Invalid response from server");
      }
    } catch (err: unknown) {
      console.error("Login error:", err);

      // Handle different error types
      if (err && typeof err === "object" && "response" in err) {
        const axiosError = err as { response?: { data?: { error_description?: string; error?: string }; status?: number } };
        if (axiosError.response?.data?.error_description) {
          setError(axiosError.response.data.error_description);
        } else if (axiosError.response?.data?.error) {
          setError(axiosError.response.data.error);
        } else if (axiosError.response?.status === 400) {
          setError("Invalid username or password");
        } else {
          setError("Authentication failed. Please try again.");
        }
      } else {
        setError("Unable to connect to server. Please check your connection.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-primary/10 to-background p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-4 text-center">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-primary">
            <Zap className="h-8 w-8 text-primary-foreground" />
          </div>
          <div>
            <CardTitle className="text-2xl">KLC Admin</CardTitle>
            <CardDescription>
              Sign in to manage your charging network
            </CardDescription>
          </div>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="flex items-center gap-2 rounded-md bg-destructive/10 p-3 text-sm text-destructive">
                <AlertCircle className="h-4 w-4 flex-shrink-0" />
                <span>{error}</span>
              </div>
            )}

            <div className="space-y-2">
              <label htmlFor="email" className="text-sm font-medium">
                Username or Email
              </label>
              <input
                id="email"
                type="text"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="admin"
                required
                autoComplete="username"
                className="h-10 w-full rounded-md border bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="password" className="text-sm font-medium">
                Password
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Enter your password"
                  required
                  autoComplete="current-password"
                  className="h-10 w-full rounded-md border bg-background px-3 pr-10 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </button>
              </div>
            </div>

            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? "Signing in..." : "Sign In"}
            </Button>
          </form>

          <div className="mt-6 rounded-md bg-muted p-4 text-sm">
            <p className="font-medium mb-2">Demo Credentials:</p>
            <div className="space-y-1 text-muted-foreground">
              <p><span className="font-medium text-foreground">Admin:</span> admin / Admin@123</p>
              <p><span className="font-medium text-foreground">Operator:</span> operator / Admin@123</p>
              <p><span className="font-medium text-foreground">Viewer:</span> viewer / Admin@123</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
