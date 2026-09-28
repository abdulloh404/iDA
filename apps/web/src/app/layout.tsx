import './global.css';

export const metadata = {
  title: 'iDA',
  description: 'iDA web application',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
