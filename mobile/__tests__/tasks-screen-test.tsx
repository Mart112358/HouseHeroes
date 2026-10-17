import { InMemoryCache } from '@apollo/client';
import { MockedProvider } from '@apollo/client/testing/react';
import { fireEvent, render, screen } from '@testing-library/react-native';

import TasksScreen from '@/app/(tabs)/index';
import { CurrentUserDocument, MyTasksDocument } from '@/graphql/generated/graphql';
import { child, guardian, task } from '../test-support/factories';

jest.mock('expo-router', () => ({ router: { push: jest.fn() } }));

const viewerMock = (viewer: typeof child) => ({
  request: { query: CurrentUserDocument },
  result: {
    data: {
      currentUser: { __typename: 'User', id: viewer.id, firstName: 'Emma', lastName: 'Tester', email: 'emma@test', role: viewer.role, familyId: 'family-1' },
    },
  },
});

const tasksMock = {
  request: { query: MyTasksDocument },
  result: {
    data: {
      myTasks: [
        task({ id: 'dishes', title: 'Empty the dishwasher', assignedTo: [child.id] }),
        task({ id: 'groceries', title: 'Buy groceries', assignedTo: [guardian.id] }),
        task({ id: 'room', title: 'Tidy your room', assignedTo: [child.id], isCompleted: true, completedAt: '2026-10-16T18:00:00Z' }),
      ],
    },
  },
};

async function renderAs(viewer: typeof child) {
  // The screen reads the viewer from the cache, as it does once the root layout has loaded it
  const cache = new InMemoryCache({ typePolicies: { TaskAssignment: { keyFields: ['taskId', 'userId'] } } });
  cache.writeQuery({ query: CurrentUserDocument, data: viewerMock(viewer).result.data });
  await render(
    <MockedProvider mocks={[tasksMock]} cache={cache}>
      <TasksScreen />
    </MockedProvider>
  );
  await screen.findAllByText(/Empty the dishwasher|Buy groceries/);
}

it("shows a child their own open tasks, and can't create tasks", async () => {
  await renderAs(child);

  expect(screen.getByText('Empty the dishwasher')).toBeOnTheScreen();
  expect(screen.queryByText('Buy groceries')).toBeNull();
  expect(screen.queryByText('Tidy your room')).toBeNull();
  expect(screen.queryByText('New task')).toBeNull();
});

it('switches between everyone and done tasks', async () => {
  await renderAs(child);

  await fireEvent.press(screen.getByText('Everyone'));
  expect(screen.getByText('Buy groceries')).toBeOnTheScreen();
  // A child can't tick off someone else's task
  expect(screen.getByLabelText('Mark "Buy groceries" as done')).toBeDisabled();

  await fireEvent.press(screen.getByText('Done'));
  expect(screen.getByText('Tidy your room')).toBeOnTheScreen();
});

it('shows guardians the whole family and the new task button', async () => {
  await renderAs(guardian);

  expect(screen.getByText('Empty the dishwasher')).toBeOnTheScreen();
  expect(screen.getByText('Buy groceries')).toBeOnTheScreen();
  expect(screen.getByText('New task')).toBeOnTheScreen();
});
