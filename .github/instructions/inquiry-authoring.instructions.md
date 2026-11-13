---
description: Mission, argument, voice, and stage boundaries for Respondeo Inquiry content
applyTo: "src/Respondeo.Content/wwwroot/inquiry/**"
---

# Inquiry Content Authoring

Inquiry is a reasoned path toward Catholicism. Its purpose is to help people recognize what is true,
work through the obstacles that keep them from it, and ultimately come to Christ and His Church. Treat
that mission seriously: the reader needs understandable arguments and real answers, not a series of
reassurances or a neutral survey that never reaches a conclusion.

## Follow the reader's stage

- Use arguments and evidence appropriate to the question the reader has reached. The stages in
  `src/Respondeo.Content/wwwroot/inquiry/flow.json` move from Why God to Which God, Why Jesus,
  Why the Church, and Coming Home. Check the actual article and its place in the journey before editing.
- Build on what has been argued up to that point; do not assume the reader has accepted conclusions
  that the journey has not yet established. Readers may also arrive directly at an article, so briefly
  explain or link to a needed earlier argument rather than assuming they have read every preceding page.
- In Why God, make the case for God's existence using the relevant philosophical arguments. Do not bring
  Mary or the saints into that case, or rely on Church authority or later revealed doctrines. Cite Aquinas
  as the thinker being explained, but explain the reasoning rather than asking the reader to accept it on his authority.
- Introduce historical, scriptural, and Catholic claims where the journey has reached and can support
  them. Do not remove the Catholic destination or flatten later stages into generic spirituality.

## Teach the argument

- Identify the question, define unfamiliar terms, explain the premises, show how the conclusion follows,
  and address the strongest relevant objection. Do not replace reasoning with a summary, assertion,
  repeated questions, or commentary about what remains to be proved.
- Explain the step a skeptical reader is likely to challenge. When a concrete example ends before the
  conclusion, show how the reasoning continues; do not simply jump from the example to God.
- Keep distinct arguments distinct. Do not silently substitute a different argument for Aquinas's,
  attribute an unsupported claim to a source, or present a disputed inference as settled. Explain a
  necessary qualification clearly and once, while still teaching the positive case.
- Do not let analogies do the work of proof. Use them to illuminate the reasoning, explain the relevant
  point of comparison, and preserve the argument that connects the example to the conclusion.
- Distinguish lack of belief, uncertainty, and a positive claim that God does not exist. Address the
  position actually presented rather than imposing a definition or shifting the burden of proof.

## Voice and readability

- Write for a real person who wants to understand: friendly, inviting, direct, and intellectually serious.
  State the case confidently where the reasoning supports it; do not weaken the argument into mere
  possibility or surround it with disclaimers.
- Avoid generic affirmation, therapeutic or HR-style language, repeated assurances that a reader's
  feelings are valid, and obvious observations that do not advance the argument. Respect the reader by
  answering the question, not by padding the page.
- Prefer plain words, short steps, concrete examples, and clear connections. Explain necessary technical
  terms in context; do not replace useful detail with jargon or oversimplification. Use headings and lists
  when they make the reasoning easier to follow, and write accessible link text and meaningful structure.
- Introduce videos briefly with their subject and how they relate to the article. Verify the subject rather
  than guessing, and avoid filler about a video being optional or speaking for itself.
- Treat objections fairly and answer them substantively. Be accurate and compassionate where the subject
  calls for it, without making emotional reassurance the argument or pretending certainty about matters
  the reasoning does not establish.

## Editing discipline

- The existing article is the baseline. Read it and its surrounding context before proposing changes;
  identify what its examples, explanations, and rhetorical flow accomplish.
- Make local improvements that solve a specific problem. Preserve the author's voice, concrete examples,
  quotations, argument structure, and useful clarifications unless a change is necessary and authorized.
  A shorter passage is not an improvement if it removes a step the reader needs.
- Do not rewrite an entire article or a group of articles merely to make style consistent. Work within
  the user's requested scope, preserve all existing routes and sections unless explicitly asked otherwise,
  and never add tracking, navigation, or unrelated features as part of a content revision.
- Before finishing, reread the edited passage as a new reader: can they follow why each step leads to the
  next, see how the objection is answered, and understand why the conclusion matters? Check the diff
  for lost arguments or examples. Follow `src/Respondeo.Content/INQUIRY.md` for content structure,
  citations, and links, as well as the repository's general writing conventions.
