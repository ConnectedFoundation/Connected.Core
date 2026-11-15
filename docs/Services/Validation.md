# Request validation

Every service call validates its request (`IDto`) before the operation runs
(`src/Connected/Services/Validation/ValidationContext.cs`, called from `Service.Validate`).

## What is walked

- The request object and, recursively, every complex public member and every item of every collection member.
- On each object: the `System.ComponentModel.DataAnnotations` attributes of its public members (`Required`, `Range`, ...),
  enum values that must be defined, and `<script>` in writable string members. Then its non-public members' attributes.
- Not walked: null, `Type`, primitives and every value type, and members marked `[SkipValidation]` (they are not even
  read).
- The first failure is thrown as a `ValidationException`; validator middleware (`IValidator<TDto>`) runs after the walk.

## How the walk ends

- **Each object once.** Objects already walked are kept by reference in a set (`ReferenceEqualityComparer`), so a cycle
  ends and an object reached twice is walked once. Telling whether an object was seen is one lookup; nothing is hashed by
  value, so a record that holds itself is safe.
- **New equal copies.** A member that hands back a new object equal to one above it on the way down (a record's
  `this with { }`, or two records handing back each other) is not walked: it would otherwise produce new references
  forever. Only the path above the current object is compared by `Equals`, so the cost is linear in the number of objects.
- **Equal objects in different places** are each validated (an invalid one among equal-looking ones fails the request).
- **Known limit:** two distinct records of the same type that refer to each other in a loop overflow the stack inside the
  compiler-generated record `Equals`, as they did before this walk was changed.

## Required members of a caller's request

C# `required` is enforced by the compiler only. A request bound from a caller (REST) is created by the DTO factory and
filled by the binder, so a member the caller left out arrives null, and the operation used to fail later with a null
reference that named nothing: the caller got "Object reference not set to an instance of an object" instead of the name
of the member it left out.

- The REST binder (`ServiceRequestDelegate`) marks every DTO it fills, also an empty one, with `RequestDtos.MarkBound`.
- When the request being validated is marked, each DTO in it (the request and DTOs nested in it) is refused when a
  `required` member whose type does not allow null is null: `The {member} field is required.`, the member in camel case
  as the request spells it. This runs after the attribute checks of the same object, before nested members are walked.
- Not checked: members whose type allows null (`required string?`, `required int?`), value types (`required int`: left
  out cannot be told from the default), empty strings (a value; attributes or the operation decide), objects in the
  request that are not DTOs (entities, models the server built), and DTOs built by code through `DtoFactory` (not
  marked), so no internal call starts failing.
- gRPC calls (`GrpcExtensions.Invoke`) are not marked: they are calls between an application's own services, not from outside callers.

Tests: `tests/Connected.Net.Tests/Validation/RequiredMembersTests.cs`.

## Large requests

A request carrying large in-memory data the server built itself (a projected view, a snapshot) should mark that member
`[SkipValidation]`. Walking it finds nothing and costs time in proportion to its size.

History: the visited check was a list searched item by item, O(n²) over the objects of the request; a request carrying
about 100k objects did not finish validating in an hour.
Tests: `tests/Connected.Net.Tests/Validation/ValidationContextTests.cs`.
