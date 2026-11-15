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

## Large requests

A request carrying large in-memory data the server built itself (a projected view, a snapshot) should mark that member
`[SkipValidation]`. Walking it finds nothing and costs time in proportion to its size.

History: RA-480 (2026-11-15). The visited check was a list searched item by item, O(n²) over the objects of the request;
a Pulse discovery job carrying 104k runs did not finish validating in an hour.
Tests: `tests/Connected.Net.Tests/Validation/ValidationContextTests.cs`.
