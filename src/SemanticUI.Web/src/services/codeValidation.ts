import * as acorn from 'acorn';
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
  'XMLHttpRequest',
  'fetch'
];

export function validateCode(code: string): ValidationResult {
  const violations: string[] = [];

  try {
    const ast = acorn.parse(code, {
      ecmaVersion: 2020,
      sourceType: 'module'
    });

    walk.simple(ast, {
      Identifier(node: acorn.Node) {
        const identifier = node as acorn.Identifier;
        if (DANGEROUS_PATTERNS.includes(identifier.name)) {
          violations.push(`Dangerous identifier: ${identifier.name}`);
        }
      },
      CallExpression(node: acorn.Node) {
        const callExpr = node as acorn.CallExpression;
        if (callExpr.callee.type === 'Identifier') {
          const callee = callExpr.callee as acorn.Identifier;
          if (callee.name === 'eval') {
            violations.push('eval() call detected - critical security risk');
          }
        }
      },
      AssignmentExpression(node: acorn.Node) {
        const assignExpr = node as acorn.AssignmentExpression;
        if (assignExpr.left.type === 'MemberExpression') {
          const memberExpr = assignExpr.left as acorn.MemberExpression;
          if (memberExpr.property.type === 'Identifier') {
            const prop = memberExpr.property as acorn.Identifier;
            if (prop.name === 'innerHTML' || prop.name === 'outerHTML') {
              violations.push(`Direct ${prop.name} assignment detected - XSS risk`);
            }
          }
        }
      }
    });

    return {
      safe: violations.length === 0,
      violations
    };
  } catch (error) {
    return {
      safe: false,
      violations: [`Parse error: ${(error as Error).message}`]
    };
  }
}
