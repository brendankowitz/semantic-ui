import { Chat } from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export class ApiService {
  private static async fetch<T>(
    endpoint: string,
    options?: RequestInit
  ): Promise<T> {
    const response = await fetch(`${API_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers
      }
    });

    if (!response.ok) {
      throw new Error(`API error: ${response.statusText}`);
    }

    return response.json();
  }

  static async getChats(): Promise<Chat[]> {
    const result = await this.fetch<{ success: boolean; data: Chat[] }>(
      '/api/chat'
    );
    return result.data;
  }

  static async getChat(chatId: string): Promise<Chat> {
    const result = await this.fetch<{ success: boolean; data: Chat }>(
      `/api/chat/${chatId}`
    );
    return result.data;
  }

  static async createChat(title?: string): Promise<Chat> {
    const result = await this.fetch<{ success: boolean; data: Chat }>(
      '/api/chat',
      {
        method: 'POST',
        body: JSON.stringify({ title })
      }
    );
    return result.data;
  }

  static async deleteChat(chatId: string): Promise<void> {
    await this.fetch(`/api/chat/${chatId}`, {
      method: 'DELETE'
    });
  }
}
