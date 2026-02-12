import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import Link from "next/link";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "MsgPulse - Message Platform Management",
  description: "Unified message sending management platform",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
      >
        <div className="min-h-screen flex">
          <nav className="w-64 bg-gray-800 text-white p-4">
            <h1 className="text-2xl font-bold mb-8">MsgPulse</h1>
            <ul className="space-y-2">
              <li>
                <Link href="/" className="block p-2 hover:bg-gray-700 rounded">
                  Dashboard
                </Link>
              </li>
              <li>
                <Link href="/manufacturers" className="block p-2 hover:bg-gray-700 rounded">
                  Manufacturers
                </Link>
              </li>
              <li>
                <Link href="/sms-templates" className="block p-2 hover:bg-gray-700 rounded">
                  SMS Templates
                </Link>
              </li>
              <li>
                <Link href="/email-templates" className="block p-2 hover:bg-gray-700 rounded">
                  Email Templates
                </Link>
              </li>
              <li>
                <Link href="/route-rules" className="block p-2 hover:bg-gray-700 rounded">
                  Route Rules
                </Link>
              </li>
              <li>
                <Link href="/messages" className="block p-2 hover:bg-gray-700 rounded">
                  Message Records
                </Link>
              </li>
            </ul>
          </nav>
          <main className="flex-1 p-8 bg-gray-50">
            {children}
          </main>
        </div>
      </body>
    </html>
  );
}
