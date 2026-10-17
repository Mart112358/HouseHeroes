import { ApolloClient, ApolloLink, HttpLink, InMemoryCache } from '@apollo/client';
import { SetContextLink } from '@apollo/client/link/context';

import { config } from '@/config';

export function createApolloClient(getAccessToken: () => Promise<string | null>) {
  const authLink = new SetContextLink(async (prevContext) => {
    const token = await getAccessToken();
    return token ? { headers: { ...prevContext.headers, authorization: `Bearer ${token}` } } : {};
  });

  return new ApolloClient({
    link: ApolloLink.from([authLink, new HttpLink({ uri: config.graphqlUrl })]),
    cache: new InMemoryCache({
      typePolicies: {
        // Assignments have no id of their own
        TaskAssignment: { keyFields: ['taskId', 'userId'] },
      },
    }),
  });
}
