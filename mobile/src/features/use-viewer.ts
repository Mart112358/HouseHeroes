import { useQuery } from '@apollo/client/react';

import { CurrentUserDocument } from '@/graphql/generated/graphql';

// The signed-in user's record. Screens inside the registered area can rely on it,
// because the root layout only shows them once currentUser has loaded.
export function useViewer() {
  const { data } = useQuery(CurrentUserDocument);
  const viewer = data?.currentUser;
  if (!viewer) throw new Error('useViewer is only available to registered users');
  return viewer;
}
