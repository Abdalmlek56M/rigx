import { Suspense } from "react";
import { LoginExperience } from "@/components/login-experience";
import { LoadingCard } from "@/components/ui";

export default function LoginPage() {
  return (
    <Suspense fallback={<LoadingCard />}>
      <LoginExperience />
    </Suspense>
  );
}
