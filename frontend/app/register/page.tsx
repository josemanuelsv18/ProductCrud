import Link from 'next/link';
import { AuthForm } from '@/components/AuthForm';

export default function RegisterPage() {
  return (
    <div className="space-y-6 py-10">
      <div className="text-center">
        <Link href="/login" className="text-sm font-medium text-brand-600">Ya tengo cuenta</Link>
      </div>
      <AuthForm mode="register" />
    </div>
  );
}
