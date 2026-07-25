import Link from 'next/link';

export default function ContactPage() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-16 max-w-3xl">
        <Link href="/" className="text-sm text-muted-foreground hover:text-foreground transition-colors mb-8 inline-block">
          &larr; Back to home
        </Link>
        <h1 className="text-3xl font-bold mb-4">Contact Us</h1>
        <p className="text-muted-foreground mb-8">
          Have a question or need help? We&apos;d love to hear from you.
        </p>
        <div className="space-y-6">
          <div className="p-6 rounded-2xl border bg-card">
            <h2 className="text-lg font-semibold mb-2">Email</h2>
            <p className="text-muted-foreground">
              For general inquiries: <a href="mailto:hello@notrelix.com" className="text-primary hover:underline">hello@notrelix.com</a>
            </p>
            <p className="text-muted-foreground mt-1">
              For support: <a href="mailto:support@notrelix.com" className="text-primary hover:underline">support@notrelix.com</a>
            </p>
          </div>
          <div className="p-6 rounded-2xl border bg-card">
            <h2 className="text-lg font-semibold mb-2">Response Time</h2>
            <p className="text-muted-foreground">
              We typically respond within 24 hours during business days.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
