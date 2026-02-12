'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';

export default function MessagesPage() {
  const [messages, setMessages] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadMessages();
  }, []);

  const loadMessages = async () => {
    try {
      const result = await api.get('/api/messages');
      if (result.code === 200) {
        setMessages(result.data.records);
      }
    } catch (error) {
      console.error('Failed to load messages:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRetry = async (id: number) => {
    try {
      await api.post(`/api/messages/${id}/retry`, {});
      loadMessages();
    } catch (error) {
      console.error('Failed to retry message:', error);
    }
  };

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      <h1 className="text-3xl font-bold mb-6">Message Records</h1>
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <table className="min-w-full">
          <thead className="bg-gray-100">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Task ID</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Template</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Recipient</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {messages.map((message) => (
              <tr key={message.id}>
                <td className="px-6 py-4 whitespace-nowrap text-sm">{message.taskId}</td>
                <td className="px-6 py-4 whitespace-nowrap">{message.messageType}</td>
                <td className="px-6 py-4 whitespace-nowrap">{message.templateCode}</td>
                <td className="px-6 py-4 whitespace-nowrap">{message.recipient}</td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`px-2 py-1 text-xs rounded ${message.sendStatus === 'Success' ? 'bg-green-100 text-green-800' : message.sendStatus === 'Failed' ? 'bg-red-100 text-red-800' : 'bg-yellow-100 text-yellow-800'}`}>
                    {message.sendStatus}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  {message.sendStatus === 'Failed' && (
                    <button onClick={() => handleRetry(message.id)} className="text-blue-600 hover:text-blue-800">
                      Retry
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
