export interface Message {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
}

export interface UIDefinition {
  componentId: string;
  type: string;
  code: string;
  props?: Record<string, unknown>;
  dependencies?: Record<string, string>;
  createdAt: Date;
}

export interface StreamChunk {
  type: ChunkType;
  content?: string;
  uiDefinition?: UIDefinition;
  metadata?: Record<string, unknown>;
}

export enum ChunkType {
  Text = 0,
  UIComponent = 1,
  Metadata = 2,
  Error = 3
}

export interface Chat {
  id: string;
  title: string;
  createdAt: Date;
  updatedAt: Date;
  messages?: Message[];
  uiComponents?: UIDefinition[];
}

export interface ValidationResult {
  safe: boolean;
  violations: string[];
}
