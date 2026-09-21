namespace API.Models;

// Minimal shapes for binding incoming JSON request bodies onto something
// typed. We are NOT formalizing a full DTO strategy yet — that is Day 2's
// topic (multiple DTO shapes, mapping strategies, why entities and wire
// formats should never be the same type). For today, these records are just
// enough to get JSON in safely and give the controller something to bind to.
//
// Notice these are records, not classes: flat, immutable data-in-transit,
// with no behavior of their own. That is the same distinction drawn on
// Student — Student is an entity that protects its own rules; these are
// just the shape of a request.

// Used by POST /api/students to create a new Student.
public record StudentCreateRequest(string FullName, string LearnerReferenceNumber);

// Used by PUT /api/students/{id} to update an existing Student's name.
public record StudentUpdateRequest(string FullName);
