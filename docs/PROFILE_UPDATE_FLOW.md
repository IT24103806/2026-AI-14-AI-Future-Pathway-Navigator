# Profile updates ("Re-run AI Onboarding")

Students can revisit their profile at any time from the dashboard button **🤖 Re-run AI Onboarding**
(`/onboarding?mode=update`) or from any "AI Onboarding" link — opening `/onboarding` with a completed
profile automatically switches to the update experience instead of bouncing back to the dashboard.

## What the student can change

| Field | Stored in | Chat (Agent 1) | Standard form |
| --- | --- | --- | --- |
| Full name | `Users.FullName` | ✅ "My name is Nimal Perera" | ✅ text input |
| Academic stage | `StudentProfiles.AcademicStage` | ✅ | ✅ dropdown |
| Core skills | `StudentProfiles.CoreSkills` | ✅ (added/merged) | ✅ tag editor |
| Hobbies & interests | `StudentProfiles.HobbiesInterests` | ✅ (added/merged) | ✅ tag editor |
| Career ambition | `StudentProfiles.CareerAmbitions` | ✅ (corrected) | ✅ textarea |
| A/L stream, results, budget | `StudentProfiles.AlStream/AlResults/BudgetLevel` | – (kept from the Reality Check flow) | – (kept) |

## How it works

1. **Load** — `GET /profile` returns the saved profile (including `Users.FullName`); the page maps it to the
   slot format Agent 1 uses (`src/utils/profileSlots.js`) and pre-fills both the chat sidebar and the form.
2. **Chat** — every turn posts `{ message, history, current_slots, baseline_slots, update_mode: true }` to
   `POST /onboarding/chat`. `baseline_slots` is the profile as stored before the session, which lets the agent
   distinguish "the student is looking around" from "the student changed something":
   * nothing changed yet → `is_complete: false`, and the agent asks what to update;
   * at least one value differs (name included) → `is_complete: true` and the backend saves the profile.
   The rule is enforced in two places so it also holds when the Python service is unavailable:
   `ai_service/agent_1_conversation/agent.py` (`evaluation_node`) and
   `backend/.../Services/AgentService.cs` (`ApplyUpdateModeGuard`).
3. **Form** — "Standard Form" is pre-filled from the same profile and saves through `PUT /profile`
   (partial update: nothing else is overwritten, in particular the Reality Check context and skills
   merged from Agent 4).
4. **Save** — `OnboardingController.ChatWithAgent1` → `StudentProfileService.SaveFromExtractedSlotsAsync`
   (chat) or `ProfileController.UpdateProfile` → `StudentProfileService.UpdateProfileAsync` (form). Both write
   `UpdatedAt` and set `IsOnboardingCompleted = true` once the four essentials are present.

## API

```
PUT /api/profile
{
  "fullName": "Nimal Jayasuriya",     // optional, written to Users.FullName
  "academicStage": "Graduated",       // optional
  "coreSkills": ["Python", "Docker"], // optional: null keeps, [] clears
  "hobbiesInterests": ["AI"],         // optional: null keeps, [] clears
  "careerAmbitions": "ML Engineer"    // optional
}
→ 200 StudentProfileDto | 400 validation | 401 unauthorized | 404 user not found
```

## Tests

* `ai_service/tests/test_agent_1.py` — update sessions only complete after a real change; name extraction never
  fires on "I am an undergraduate"; the caller's baseline profile is never mutated.
* `backend/PathwayNavigator.Tests/StudentProfileServiceTests.cs` — partial updates, full-name propagation,
  list replacement/clearing, profile auto-creation, chat-slot persistence.
* `web/PathwayNavigator/src/pages/OnboardingPage.test.jsx` — the update page loads the saved profile and no
  longer redirects away; first-time onboarding is unaffected.
* `web/PathwayNavigator/src/components/onboarding/StandardOnboardingForm.test.jsx` — pre-fill, `PUT` on update,
  `POST` on first run, and the full-name validation.
