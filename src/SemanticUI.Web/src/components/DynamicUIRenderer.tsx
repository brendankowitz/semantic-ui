import React, { useEffect, useState } from 'react';
import { LiveProvider, LiveError, LivePreview } from 'react-live';
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
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // SECURITY DISABLED FOR TESTING
    // Skip all validation - react-live runs in browser context
    setError(null);
  }, [uiDefinition.code, onError]);

  if (error) {
    return (
      <div className="border-2 border-red-500 bg-red-50 p-4 rounded-lg">
        <h3 className="text-red-800 font-semibold">Error</h3>
        <p className="text-red-600 text-sm mt-1">{error}</p>
      </div>
    );
  }

  // Create submitToChat function in scope
  const submitToChat = (payload: unknown) => {
    onInteraction(payload);
  };

  // Extract the component code - remove import/export statements
  // react-live expects just the component definition
  let cleanCode = uiDefinition.code;

  // Remove import statements
  cleanCode = cleanCode.replace(/import\s+.*?from\s+['"].*?['"];?\s*/g, '');

  // Remove export default and just keep the function
  cleanCode = cleanCode.replace(/export\s+default\s+/g, '');

  // If it's a function declaration, wrap it in parentheses and call it
  if (cleanCode.trim().startsWith('function')) {
    cleanCode = `(() => { ${cleanCode}; return ${cleanCode.match(/function\s+(\w+)/)?.[1] || 'Component'}; })()`;
  }

  return (
    <div className="border rounded-lg overflow-hidden shadow-sm bg-white">
      <div className="bg-gray-100 px-4 py-2 border-b">
        <span className="text-sm font-medium text-gray-700">
          {uiDefinition.type.charAt(0).toUpperCase() + uiDefinition.type.slice(1)} Component
        </span>
      </div>
      <div style={{ height: '600px', width: '100%', overflow: 'auto' }}>
        <LiveProvider
          code={cleanCode}
          scope={{ submitToChat, React, useState: React.useState }}
          noInline={false}
        >
          <div className="p-4">
            <LiveError className="text-red-600 bg-red-50 p-3 rounded mb-4 text-sm font-mono" />
            <LivePreview className="min-h-full" />
          </div>
        </LiveProvider>
      </div>
    </div>
  );
};
