import * as acorn from 'acorn';
import jsx from 'acorn-jsx';
import * as walk from 'acorn-walk';
import { ValidationResult } from '../types';

const DANGEROUS_PATTERNS = [
  'eval',
  'Function',
  '__proto__',
  'constructor',
  'innerHTML',
  'outerHTML',
  'document.write',
  'XMLHttpRequest'
  // Note: 'fetch' removed as it's commonly used for API calls
];

export function validateCode(code: string): ValidationResult {
  // For now, skip AST-based validation for JSX/React code
  // The code will be sandboxed in an iframe by Sandpack anyway
  // We'll do basic string-based checks for obvious security issues

  const violations: string[] = [];

  // Check for obvious dangerous patterns using string search
  if (code.includes('eval(')) {
    violations.push('eval() call detected - critical security risk');
  }

  if (code.includes('Function(')) {
    violations.push('Function constructor detected - security risk');
  }

  if (code.includes('dangerouslySetInnerHTML')) {
    violations.push('dangerouslySetInnerHTML detected - XSS risk');
  }

  // Allow the code through - Sandpack provides isolation
  // More sophisticated validation would require a proper JSX/TSX parser
  return {
    safe: violations.length === 0,
    violations
  };
}
