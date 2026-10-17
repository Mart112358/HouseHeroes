import { MockedProvider } from '@apollo/client/testing/react';
import { fireEvent, render, screen } from '@testing-library/react-native';

import WelcomeScreen from '@/app/welcome';
import { CurrentUserDocument, RegisterNewUserDocument } from '@/graphql/generated/graphql';

jest.mock('@/auth/auth-provider', () => ({ useAuth: () => ({ signOut: jest.fn() }) }));

const notRegistered = { request: { query: CurrentUserDocument }, result: { data: { currentUser: null } } };

function registerMock(input: object, result: { success: boolean; message: string }) {
  return {
    request: { query: RegisterNewUserDocument, variables: { input } },
    result: {
      data: {
        registerNewUser: { __typename: 'RegisterNewUserResult', ...result, user: result.success ? { __typename: 'User', id: 'user-1' } : null },
      },
    },
  };
}

it('creates a family with the entered name', async () => {
  const register = registerMock({ createNewFamily: true, familyName: 'The Testers' }, { success: true, message: 'ok' });
  await render(
    <MockedProvider mocks={[notRegistered, register, notRegistered]}>
      <WelcomeScreen />
    </MockedProvider>
  );

  await fireEvent.changeText(await screen.findByLabelText('Family name'), '  The Testers ');
  await fireEvent.press(screen.getByText('Create family'));

  // The mock only matches the trimmed name; a mismatch would surface as an error message
  expect(await screen.findByText('Welcome!')).toBeOnTheScreen();
  expect(screen.queryByText(/couldn.t reach/i)).toBeNull();
});

it('explains when an invite code is rejected', async () => {
  const register = registerMock(
    { createNewFamily: false, inviteCode: 'BADC0DE1' },
    { success: false, message: 'This invite code is invalid, already used, or expired.' }
  );
  await render(
    <MockedProvider mocks={[notRegistered, register, notRegistered]}>
      <WelcomeScreen />
    </MockedProvider>
  );

  await fireEvent.changeText(await screen.findByLabelText('Invite code'), 'BADC0DE1');
  await fireEvent.press(screen.getByText('Join family'));

  expect(await screen.findByText('This invite code is invalid, already used, or expired.')).toBeOnTheScreen();
});
