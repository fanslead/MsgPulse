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
  title: "MsgPulse - 消息平台管理系统",
  description: "统一的消息发送管理平台",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="zh-CN">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
      >
        <div className="min-h-screen flex">
          {/* 侧边导航栏 - 毛玻璃效果 */}
          <nav className="w-64 glass p-6 shadow-2xl">
            <div className="mb-8">
              <h1 className="text-2xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
                MsgPulse
              </h1>
              <p className="text-xs text-slate-400 mt-1">消息平台管理系统</p>
            </div>
            <ul className="space-y-1">
              <li>
                <Link
                  href="/"
                  className="block px-4 py-2.5 rounded-lg hover:bg-white/10 transition-all duration-200 text-slate-200 hover:text-white hover:translate-x-1"
                >
                  <span className="inline-block mr-2">📊</span>
                  仪表板
                </Link>
              </li>
              <li>
                <Link
                  href="/channels"
                  className="block px-4 py-2.5 rounded-lg hover:bg-white/10 transition-all duration-200 text-slate-200 hover:text-white hover:translate-x-1"
                >
                  <span className="inline-block mr-2">📡</span>
                  渠道管理
                </Link>
              </li>
              <li>
                <Link
                  href="/manufacturers"
                  className="block px-4 py-2.5 rounded-lg hover:bg-white/10 transition-all duration-200 text-slate-200 hover:text-white hover:translate-x-1"
                >
                  <span className="inline-block mr-2">🏭</span>
                  厂商管理(旧)
                </Link>
              </li>
              <li>
                <Link
                  href="/sms-templates"
                  className="block px-4 py-2.5 rounded-lg hover:bg-white/10 transition-all duration-200 text-slate-200 hover:text-white hover:translate-x-1"
                >
                  <span className="inline-block mr-2">📱</span>
                  短信模板
                </Link>
              </li>
              <li>
                <Link
                  href="/email-templates"
                  className="block px-4 py-2.5 rounded-lg hover:bg-white/10 transition-all duration-200 text-slate-200 hover:text-white hover:translate-x-1"
                >
                  <span className="inline-block mr-2">✉️</span>
                  邮件模板
                </Link>
              </li>
              <li>
                <Link
                  href="/route-rules"
                  className="block px-4 py-2.5 rounded-lg hover:bg-white/10 transition-all duration-200 text-slate-200 hover:text-white hover:translate-x-1"
                >
                  <span className="inline-block mr-2">🔀</span>
                  路由规则
                </Link>
              </li>
              <li>
                <Link
                  href="/messages"
                  className="block px-4 py-2.5 rounded-lg hover:bg-white/10 transition-all duration-200 text-slate-200 hover:text-white hover:translate-x-1"
                >
                  <span className="inline-block mr-2">📨</span>
                  消息记录
                </Link>
              </li>
            </ul>
          </nav>

          {/* 主内容区域 */}
          <main className="flex-1 p-8 overflow-auto">
            {children}
          </main>
        </div>
      </body>
    </html>
  );
}
