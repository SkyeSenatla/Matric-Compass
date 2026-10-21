namespace API.Models;

// Week 7 Day 3: "who am I?", answered from the access token. Email and role
// only: everything the frontend needs to choose a screen, nothing more.
public record MeResponse(string Email, string Role);
