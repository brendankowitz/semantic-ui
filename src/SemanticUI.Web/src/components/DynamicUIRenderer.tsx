import React, { useEffect, useState } from 'react';
import { Sandpack } from '@codesandbox/sandpack-react';
import DOMPurify from 'dompurify';
import { validateCode } from '../services/codeValidation';
import { UIDefinition } from '../types';

interface DynamicUIRendererProps {
  uiDefinition: UIDefinition;
  onInteraction: (payload: unknown) => void;
  onError?: (error: string) => void;
}

export const DynamicUIRenderer: React.FC<DynamicUIRendererProps> = ({
  uiDefinition,
  onInteraction,
  onError
}) => {
  const [validatedCode, setValidatedCode] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function processCode() {
      try {
        // Step 1: AST Validation
        const astResult = validateCode(uiDefinition.code);
        if (!astResult.safe) {
          const errorMsg = `Security violation: ${astResult.violations.join(', ')}`;
          setError(errorMsg);
          onError?.(errorMsg);
          return;
        }

        // Step 2: DOMPurify Sanitization
        const sanitized = DOMPurify.sanitize(uiDefinition.code, {
          ALLOWED_TAGS: [],
          KEEP_CONTENT: true
        });

        // Step 3: Verify no content was removed
        if (sanitized !== uiDefinition.code) {
          const errorMsg = 'Potentially malicious HTML detected';
          setError(errorMsg);
          onError?.(errorMsg);
          return;
        }

        setValidatedCode(sanitized);
        setError(null);
      } catch (err) {
        const errorMsg = `Validation error: ${(err as Error).message}`;
        setError(errorMsg);
        onError?.(errorMsg);
      }
    }

    processCode();
  }, [uiDefinition.code, onError]);

  useEffect(() => {
    // Listen for postMessage from iframe
    const handleMessage = (event: MessageEvent) => {
      if (
        event.data.type === 'UI_INTERACTION' &&
        event.data.componentId === uiDefinition.componentId
      ) {
        onInteraction(event.data.payload);
      }
    };

    window.addEventListener('message', handleMessage);
    return () => window.removeEventListener('message', handleMessage);
  }, [uiDefinition.componentId, onInteraction]);

  if (error) {
    return (
      <div className="border-2 border-red-500 bg-red-50 p-4 rounded-lg">
        <h3 className="text-red-800 font-semibold">Security Error</h3>
        <p className="text-red-600 text-sm mt-1">{error}</p>
      </div>
    );
  }

  if (!validatedCode) {
    return (
      <div className="animate-pulse bg-gray-100 p-4 rounded-lg">
        <div className="h-4 bg-gray-300 rounded w-3/4 mb-2"></div>
        <div className="h-4 bg-gray-300 rounded w-1/2"></div>
      </div>
    );
  }

  const utilsCode = `
export function submitToChat(payload) {
  window.parent.postMessage({
    type: 'UI_INTERACTION',
    componentId: '${uiDefinition.componentId}',
    payload
  }, '*');
}
`;

  return (
    <div className="border rounded-lg overflow-hidden shadow-sm bg-white">
      <div className="bg-gray-100 px-4 py-2 border-b">
        <span className="text-sm font-medium text-gray-700">
          {uiDefinition.type.charAt(0).toUpperCase() + uiDefinition.type.slice(1)} Component
        </span>
      </div>
      <Sandpack
        template="react"
        files={{
          '/App.js': validatedCode,
          '/utils.js': utilsCode
        }}
        customSetup={{
          dependencies: {
            react: '^18.2.0',
            'react-dom': '^18.2.0',
            ...uiDefinition.dependencies
          }
        }}
        options={{
          showNavigator: false,
          showTabs: false,
          showLineNumbers: false,
          editorHeight: 0,
          editorWidthPercentage: 0,
          externalResources: [
            'https://cdn.tailwindcss.com'
          ]
        }}
        theme="light"
      />
    </div>
  );
};
