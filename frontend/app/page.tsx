export default function Home() {
  return (
    <div>
      <h1 className="text-3xl font-bold mb-6">Dashboard</h1>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div className="bg-white p-6 rounded-lg shadow">
          <h2 className="text-xl font-semibold mb-2">Manufacturers</h2>
          <p className="text-gray-600">Manage message vendors and channels</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <h2 className="text-xl font-semibold mb-2">Templates</h2>
          <p className="text-gray-600">Configure SMS and Email templates</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <h2 className="text-xl font-semibold mb-2">Route Rules</h2>
          <p className="text-gray-600">Set up message routing logic</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <h2 className="text-xl font-semibold mb-2">Message Records</h2>
          <p className="text-gray-600">View and manage sent messages</p>
        </div>
      </div>
    </div>
  );
}
