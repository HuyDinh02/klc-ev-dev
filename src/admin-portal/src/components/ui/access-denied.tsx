import { ShieldX } from "lucide-react";
import Link from "next/link";
import { useTranslation } from "@/lib/i18n";
import { Button } from "./button";

export function AccessDenied() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 p-6">
      <ShieldX className="h-16 w-16 text-muted-foreground" />
      <h1 className="text-2xl font-semibold">{t("errors.accessDenied")}</h1>
      <p className="text-center text-muted-foreground">{t("errors.accessDeniedDescription")}</p>
      <Link href="/">
        <Button variant="outline">{t("errors.backToDashboard")}</Button>
      </Link>
    </div>
  );
}
