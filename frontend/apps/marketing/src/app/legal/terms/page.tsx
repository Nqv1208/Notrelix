import Link from "next/link";

export default function TermsPage() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-16 max-w-3xl">
        <Link
          href="/"
          className="text-sm text-muted-foreground hover:text-foreground transition-colors mb-8 inline-block"
        >
          &larr; Back to home
        </Link>
        <h1 className="text-3xl font-bold mb-8">Terms of Service</h1>
        <div className="prose prose-neutral dark:prose-invert max-w-none space-y-6">
          <p>Last updated: {new Date().toLocaleDateString()}</p>
          <h2>1. Acceptance of Terms</h2>
          <p>
            By accessing or using Notrelix, you agree to be bound by these Terms
            of Service. If you do not agree to these terms, please do not use
            our service.
          </p>
          <h2>2. Use of Service</h2>
          <p>
            You may use our service only for lawful purposes and in accordance
            with these Terms. You are responsible for maintaining the
            confidentiality of your account credentials.
          </p>
          <h2>3. Intellectual Property</h2>
          <p>
            The service and its original content, features, and functionality
            are owned by Notrelix and are protected by international copyright,
            trademark, patent, trade secret, and other intellectual property
            laws.
          </p>
          <h2>4. User Content</h2>
          <p>
            You retain ownership of any content you create or upload to
            Notrelix. By using our service, you grant us a limited license to
            host, store, and display your content solely for the purpose of
            providing the service.
          </p>
          <h2>5. Termination</h2>
          <p>
            We may terminate or suspend your access to our service immediately,
            without prior notice or liability, for any reason whatsoever,
            including without limitation if you breach the Terms.
          </p>
          <h2>6. Limitation of Liability</h2>
          <p>
            In no event shall Notrelix be liable for any indirect, incidental,
            special, consequential, or punitive damages resulting from your use
            of or inability to use the service.
          </p>
          <h2>7. Changes to Terms</h2>
          <p>
            We reserve the right to modify or replace these Terms at any time.
            If a revision is material, we will try to provide at least 30 days
            notice.
          </p>
          <h2>8. Contact Us</h2>
          <p>
            If you have any questions about these Terms, please contact us at
            <a
              href="mailto:legal@notrelix.com"
              className="text-primary hover:underline"
            >
              {" "}
              legal@notrelix.com
            </a>
            .
          </p>
        </div>
      </div>
    </div>
  );
}
