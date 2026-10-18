/* eslint-disable */
import * as types from './graphql';
import type { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';

/**
 * Map of all GraphQL operations in the project.
 *
 * This map has several performance disadvantages:
 * 1. It is not tree-shakeable, so it will include all operations in the project.
 * 2. It is not minifiable, so the string of a GraphQL query will be multiple times inside the bundle.
 * 3. It does not support dead code elimination, so it will add unused operations.
 *
 * Therefore it is highly recommended to use the babel or swc plugin for production.
 * Learn more about it here: https://the-guild.dev/graphql/codegen/plugins/presets/preset-client#reducing-bundle-size
 */
type Documents = {
    "query FamilyMembers {\n  myFamily {\n    id\n    name\n  }\n  familyMembers {\n    id\n    firstName\n    lastName\n    email\n    role\n  }\n}\n\nmutation CreateFamilyInvite($role: UserRole!) {\n  createFamilyInvite(role: $role) {\n    id\n    code\n    role\n    expiresAt\n  }\n}\n\nmutation UpdateFamilyMember($input: UpdateFamilyMemberInput!) {\n  updateFamilyMember(input: $input) {\n    id\n    firstName\n    lastName\n    role\n  }\n}\n\nmutation RemoveFamilyMember($userId: UUID!) {\n  removeFamilyMember(userId: $userId)\n}": typeof types.FamilyMembersDocument,
    "fragment TaskFields on Task {\n  id\n  title\n  description\n  dueDate\n  isCompleted\n  completedAt\n  taskAssignments {\n    taskId\n    userId\n    user {\n      id\n      firstName\n    }\n  }\n}\n\nquery MyTasks {\n  myTasks {\n    ...TaskFields\n  }\n}\n\nquery TaskById($id: UUID!) {\n  taskById(id: $id) {\n    ...TaskFields\n  }\n}\n\nmutation CreateTask($input: CreateTaskInput!) {\n  createTask(input: $input) {\n    id\n  }\n}\n\nmutation UpdateTask($input: UpdateTaskInput!) {\n  updateTask(input: $input) {\n    ...TaskFields\n  }\n}\n\nmutation CompleteTask($taskId: UUID!) {\n  completeTask(taskId: $taskId) {\n    id\n    isCompleted\n    completedAt\n  }\n}\n\nmutation ReopenTask($taskId: UUID!) {\n  reopenTask(taskId: $taskId) {\n    id\n    isCompleted\n    completedAt\n  }\n}\n\nmutation DeleteTask($taskId: UUID!) {\n  deleteTask(taskId: $taskId)\n}": typeof types.TaskFieldsFragmentDoc,
    "query CurrentUser {\n  currentUser {\n    id\n    firstName\n    lastName\n    email\n    role\n    familyId\n  }\n}\n\nmutation RegisterNewUser($input: RegisterNewUserInput!) {\n  registerNewUser(input: $input) {\n    success\n    message\n    user {\n      id\n    }\n  }\n}\n\nmutation DeleteMyAccount {\n  deleteMyAccount\n}": typeof types.CurrentUserDocument,
};
const documents: Documents = {
    "query FamilyMembers {\n  myFamily {\n    id\n    name\n  }\n  familyMembers {\n    id\n    firstName\n    lastName\n    email\n    role\n  }\n}\n\nmutation CreateFamilyInvite($role: UserRole!) {\n  createFamilyInvite(role: $role) {\n    id\n    code\n    role\n    expiresAt\n  }\n}\n\nmutation UpdateFamilyMember($input: UpdateFamilyMemberInput!) {\n  updateFamilyMember(input: $input) {\n    id\n    firstName\n    lastName\n    role\n  }\n}\n\nmutation RemoveFamilyMember($userId: UUID!) {\n  removeFamilyMember(userId: $userId)\n}": types.FamilyMembersDocument,
    "fragment TaskFields on Task {\n  id\n  title\n  description\n  dueDate\n  isCompleted\n  completedAt\n  taskAssignments {\n    taskId\n    userId\n    user {\n      id\n      firstName\n    }\n  }\n}\n\nquery MyTasks {\n  myTasks {\n    ...TaskFields\n  }\n}\n\nquery TaskById($id: UUID!) {\n  taskById(id: $id) {\n    ...TaskFields\n  }\n}\n\nmutation CreateTask($input: CreateTaskInput!) {\n  createTask(input: $input) {\n    id\n  }\n}\n\nmutation UpdateTask($input: UpdateTaskInput!) {\n  updateTask(input: $input) {\n    ...TaskFields\n  }\n}\n\nmutation CompleteTask($taskId: UUID!) {\n  completeTask(taskId: $taskId) {\n    id\n    isCompleted\n    completedAt\n  }\n}\n\nmutation ReopenTask($taskId: UUID!) {\n  reopenTask(taskId: $taskId) {\n    id\n    isCompleted\n    completedAt\n  }\n}\n\nmutation DeleteTask($taskId: UUID!) {\n  deleteTask(taskId: $taskId)\n}": types.TaskFieldsFragmentDoc,
    "query CurrentUser {\n  currentUser {\n    id\n    firstName\n    lastName\n    email\n    role\n    familyId\n  }\n}\n\nmutation RegisterNewUser($input: RegisterNewUserInput!) {\n  registerNewUser(input: $input) {\n    success\n    message\n    user {\n      id\n    }\n  }\n}\n\nmutation DeleteMyAccount {\n  deleteMyAccount\n}": types.CurrentUserDocument,
};

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 *
 *
 * @example
 * ```ts
 * const query = graphql(`query GetUser($id: ID!) { user(id: $id) { name } }`);
 * ```
 *
 * The query argument is unknown!
 * Please regenerate the types.
 */
export function graphql(source: string): unknown;

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "query FamilyMembers {\n  myFamily {\n    id\n    name\n  }\n  familyMembers {\n    id\n    firstName\n    lastName\n    email\n    role\n  }\n}\n\nmutation CreateFamilyInvite($role: UserRole!) {\n  createFamilyInvite(role: $role) {\n    id\n    code\n    role\n    expiresAt\n  }\n}\n\nmutation UpdateFamilyMember($input: UpdateFamilyMemberInput!) {\n  updateFamilyMember(input: $input) {\n    id\n    firstName\n    lastName\n    role\n  }\n}\n\nmutation RemoveFamilyMember($userId: UUID!) {\n  removeFamilyMember(userId: $userId)\n}"): (typeof documents)["query FamilyMembers {\n  myFamily {\n    id\n    name\n  }\n  familyMembers {\n    id\n    firstName\n    lastName\n    email\n    role\n  }\n}\n\nmutation CreateFamilyInvite($role: UserRole!) {\n  createFamilyInvite(role: $role) {\n    id\n    code\n    role\n    expiresAt\n  }\n}\n\nmutation UpdateFamilyMember($input: UpdateFamilyMemberInput!) {\n  updateFamilyMember(input: $input) {\n    id\n    firstName\n    lastName\n    role\n  }\n}\n\nmutation RemoveFamilyMember($userId: UUID!) {\n  removeFamilyMember(userId: $userId)\n}"];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "fragment TaskFields on Task {\n  id\n  title\n  description\n  dueDate\n  isCompleted\n  completedAt\n  taskAssignments {\n    taskId\n    userId\n    user {\n      id\n      firstName\n    }\n  }\n}\n\nquery MyTasks {\n  myTasks {\n    ...TaskFields\n  }\n}\n\nquery TaskById($id: UUID!) {\n  taskById(id: $id) {\n    ...TaskFields\n  }\n}\n\nmutation CreateTask($input: CreateTaskInput!) {\n  createTask(input: $input) {\n    id\n  }\n}\n\nmutation UpdateTask($input: UpdateTaskInput!) {\n  updateTask(input: $input) {\n    ...TaskFields\n  }\n}\n\nmutation CompleteTask($taskId: UUID!) {\n  completeTask(taskId: $taskId) {\n    id\n    isCompleted\n    completedAt\n  }\n}\n\nmutation ReopenTask($taskId: UUID!) {\n  reopenTask(taskId: $taskId) {\n    id\n    isCompleted\n    completedAt\n  }\n}\n\nmutation DeleteTask($taskId: UUID!) {\n  deleteTask(taskId: $taskId)\n}"): (typeof documents)["fragment TaskFields on Task {\n  id\n  title\n  description\n  dueDate\n  isCompleted\n  completedAt\n  taskAssignments {\n    taskId\n    userId\n    user {\n      id\n      firstName\n    }\n  }\n}\n\nquery MyTasks {\n  myTasks {\n    ...TaskFields\n  }\n}\n\nquery TaskById($id: UUID!) {\n  taskById(id: $id) {\n    ...TaskFields\n  }\n}\n\nmutation CreateTask($input: CreateTaskInput!) {\n  createTask(input: $input) {\n    id\n  }\n}\n\nmutation UpdateTask($input: UpdateTaskInput!) {\n  updateTask(input: $input) {\n    ...TaskFields\n  }\n}\n\nmutation CompleteTask($taskId: UUID!) {\n  completeTask(taskId: $taskId) {\n    id\n    isCompleted\n    completedAt\n  }\n}\n\nmutation ReopenTask($taskId: UUID!) {\n  reopenTask(taskId: $taskId) {\n    id\n    isCompleted\n    completedAt\n  }\n}\n\nmutation DeleteTask($taskId: UUID!) {\n  deleteTask(taskId: $taskId)\n}"];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(source: "query CurrentUser {\n  currentUser {\n    id\n    firstName\n    lastName\n    email\n    role\n    familyId\n  }\n}\n\nmutation RegisterNewUser($input: RegisterNewUserInput!) {\n  registerNewUser(input: $input) {\n    success\n    message\n    user {\n      id\n    }\n  }\n}\n\nmutation DeleteMyAccount {\n  deleteMyAccount\n}"): (typeof documents)["query CurrentUser {\n  currentUser {\n    id\n    firstName\n    lastName\n    email\n    role\n    familyId\n  }\n}\n\nmutation RegisterNewUser($input: RegisterNewUserInput!) {\n  registerNewUser(input: $input) {\n    success\n    message\n    user {\n      id\n    }\n  }\n}\n\nmutation DeleteMyAccount {\n  deleteMyAccount\n}"];

export function graphql(source: string) {
  return (documents as any)[source] ?? {};
}

export type DocumentType<TDocumentNode extends DocumentNode<any, any>> = TDocumentNode extends DocumentNode<  infer TType,  any>  ? TType  : never;