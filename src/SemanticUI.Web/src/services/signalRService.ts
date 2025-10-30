import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState
} from '@microsoft/signalr';
import { Message, StreamChunk, UIDefinition } from '../types';

export class ChatSignalRService {
  private connection: HubConnection | null = null;
  private onMessageCallback?: (message: Message) => void;
  private onUIComponentCallback?: (ui: UIDefinition) => void;
  private onErrorCallback?: (error: string) => void;

  async initialize(hubUrl: string, token?: string): Promise<void> {
    const builder = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token || ''
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 1s, 2s, 4s, 8s, max 30s
          return Math.min(
            1000 * Math.pow(2, retryContext.previousRetryCount),
            30000
          );
        }
      });

    this.connection = builder.build();

    // Set up event handlers
    this.connection.on('ReceiveMessage', (message: Message) => {
      this.onMessageCallback?.(message);
    });

    this.connection.on('ReceiveUIComponent', (uiDef: UIDefinition) => {
      this.onUIComponentCallback?.(uiDef);
    });

    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
    });

    this.connection.onclose((error) => {
      console.error('SignalR connection closed:', error);
      this.onErrorCallback?.('Connection closed');
    });

    try {
      await this.connection.start();
      console.log('SignalR connected');
    } catch (error) {
      console.error('Failed to connect to SignalR:', error);
      throw error;
    }
  }

  async joinChat(chatId: string): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('JoinChatRoom', chatId);
    }
  }

  async leaveChat(chatId: string): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveChatRoom', chatId);
    }
  }

  async streamMessage(chatId: string, prompt: string): Promise<void> {
    if (this.connection?.state !== HubConnectionState.Connected) {
      throw new Error('SignalR not connected');
    }

    try {
      const stream = this.connection.stream<StreamChunk>(
        'StreamChatResponse',
        chatId,
        prompt
      );

      const subscription = stream.subscribe({
        next: (chunk: StreamChunk) => {
          if (chunk.type === 1 && chunk.uiDefinition) {
            // UIComponent
            this.onUIComponentCallback?.(chunk.uiDefinition);
          } else if (chunk.type === 0 && chunk.content) {
            // Text
            this.onMessageCallback?.({
              id: crypto.randomUUID(),
              role: 'assistant',
              content: chunk.content,
              timestamp: new Date()
            });
          } else if (chunk.type === 3) {
            // Error
            this.onErrorCallback?.(chunk.content || 'Unknown error');
          }
        },
        complete: () => {
          console.log('Stream complete');
        },
        error: (err) => {
          console.error('Stream error:', err);
          this.onErrorCallback?.(err.message || 'Stream error');
        }
      });
    } catch (error) {
      console.error('Failed to stream message:', error);
      throw error;
    }
  }

  async submitUIInteraction(
    componentId: string,
    payload: unknown
  ): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('SubmitUIPayload', componentId, payload);
    }
  }

  onMessage(callback: (message: Message) => void): void {
    this.onMessageCallback = callback;
  }

  onUIComponent(callback: (ui: UIDefinition) => void): void {
    this.onUIComponentCallback = callback;
  }

  onError(callback: (error: string) => void): void {
    this.onErrorCallback = callback;
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
  }

  isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }
}
