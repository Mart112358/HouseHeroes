import type { CodegenConfig } from '@graphql-codegen/cli';

// Generates typed documents for every operation in src/graphql/*.graphql.
// schema.graphql is written by the API tests (see tests/HouseHeroes.ApiService.Tests/SchemaTests.cs).
const config: CodegenConfig = {
  schema: 'schema.graphql',
  documents: ['src/graphql/*.graphql'],
  generates: {
    'src/graphql/generated/': {
      preset: 'client',
      presetConfig: { fragmentMasking: false },
      config: {
        scalars: { UUID: 'string', DateTime: 'string' },
        enumsAsTypes: true,
        useTypeImports: true,
      },
    },
  },
};

export default config;
