# Architecture Reference

Detailed rules and conventions for React/TypeScript frontend architecture.

## Technology Stack

- **Build Tool**: Vite (latest stable version)
- **Framework**: React (latest stable version)
- **Language**: TypeScript (mandatory for all new code)
- **Target**: Modern browsers

## Folder Structure

Follow a **feature-based folder structure**:

```
src/
├── components/          # Shared/reusable UI components
│   ├── common/         # Generic components
│   └── [feature]/      # Feature-specific components
├── hooks/              # Custom React hooks
├── services/           # API clients and external communication
├── lib/                # Third-party integrations and configurations
├── types/              # TypeScript type definitions
├── utils/              # Pure helper functions
├── pages/              # Route/page components
└── constants/          # Application constants and route definitions
```

## Separation of Concerns

### Component Responsibilities
- **UI Components**: Focus on rendering JSX with minimal logic
- **Custom Hooks**: Handle state management, side effects, and complex logic
- **Services**: Manage API calls and external communication
- **Utils**: Contain pure functions without side effects

### Component Splitting Rules
- Break down large or complex components into smaller, reusable pieces
- Extract logic into custom hooks when components grow complex
- Keep component files under 200 lines when possible
- Split pages into smaller components and custom hooks

### File Organization Best Practices
- Group related files by feature, not by type
- Co-locate tests with the code they test
- Keep deeply nested folder structures shallow (max 3-4 levels)
- Use index files sparingly (only for public API exports)

## TypeScript Configuration

- Use strict mode: `"strict": true` in tsconfig.json
- Avoid `any` type - use `unknown` or proper typing
- Use type inference when obvious, explicit types when needed
- Define interfaces for component props
- Use enums or const assertions for constants

## Dependencies

- Keep dependencies minimal and justified
- Regularly update dependencies for security
- Review bundle size impact of new dependencies
- Prefer well-maintained, popular libraries
- Avoid redundant dependencies

## When Creating New Features

1. **Identify the domain**: Determine which feature area the code belongs to
2. **Create feature folder**: If it doesn't exist, create a folder in `components/[feature]`
3. **Organize by concern**:
   - UI components in the feature folder
   - Business logic in custom hooks
   - API calls in services
   - Types in the types folder
4. **Keep it modular**: Each module should have a single, well-defined purpose
5. **Document complex logic**: Add JSDoc comments for non-obvious functionality
