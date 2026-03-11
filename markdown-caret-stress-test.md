# Markdown Caret Stress Test

This file is meant to stress caret placement, selection painting, visual/source mapping, and long-line drift.

## 1. Plain Long Lines

Short line.

This is a plain long line without any formatting and it should be useful to test whether the caret slowly drifts to the right as more characters are entered across a long paragraph of normal visible text with spaces punctuation and repeated words for measurement stability.

ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789 ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789 ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789

0123456789 0123456789 0123456789 0123456789 0123456789 0123456789 0123456789 0123456789 0123456789 0123456789 0123456789 0123456789

## 2. Inline Code Drift

My Name is `MDCT` a interactive Markdown Control

This is a line with `inline code` in the middle and more text after it so the caret can be checked before the backtick span inside the code span at the end of the code span and in the words after the code span.

This `line` contains `many` short `code` spans `that` may `trigger` small `offset` jumps `between` normal `text` and code `runs` repeatedly.

`leading code span` then plain text after it to see whether the first transition is stable across typing and deleting characters near the beginning of the line.

Plain text before the last `code span at the very end`

## 3. Bold Italic Strike Mix

This line has **bold text** in the middle and then plain text after it for testing.

This line has *italic text* in the middle and then plain text after it for testing.

This line has ~~strike text~~ in the middle and then plain text after it for testing.

This line has **bold** then *italic* then ~~strike~~ then `code` then plain text to verify every transition point.

This line has ***bold italic*** mixed with normal text and then `code` at the end for one more transition case.

## 4. Escapes And Hidden Markers

Escaped markers should remain visible: \*star\* \_underscore\_ \~tilde\~ \`backtick\` \\backslash\\

Escaped pipes in plain text: a \| b \| c \| d

Escaped pipes with formatting: left \| **bold** \| `code` \| right

## 5. Table Raw Lines

| Test 0 | Test 1 | Test 2 |
| --- | --- | --- |
| Alpha | Beta | Gamma |
| One more longer cell | Another cell | Final pipe check |

| A | B | C | D |
| --- | --- | --- | --- |
| 1 | 22 | 333 | 4444 |
| left | center-ish | right-ish | trailing pipe |
| code `A` | bold **B** | strike ~~C~~ | plain D |

| Empty | Middle | Last |
| --- | --- | --- |
| value |  | end |
|  | empty start | end |
| only | test | |

## 6. Lists

- Simple unordered item with plain text.
- Item with `inline code` inside the text for transition testing.
- Item with **bold** and *italic* and ~~strike~~ mixed together.

1. Ordered item one with plain text.
2. Ordered item two with `code`.
3. Ordered item three with **bold** then plain text after it.

- [ ] Task item unchecked with plain text.
- [x] Task item checked with plain text.
- [ ] Task item with `code` and **bold** and trailing text after the markers.

## 7. Quote And Admonition Style

> Simple quote line with plain text.
> Quote line with `inline code` and more plain text after it.
> Quote line with **bold** then *italic* then ~~strike~~ then plain text.

> [!NOTE]
> Admonition marker line for rich rendering.
> Follow-up text with `code` inside the admonition block.

## 8. Heading Prefix Cases

# Heading one line with plain text after marker

## Heading two line with `code` after marker

### Heading three line with **bold** and plain text

#### Heading four line with *italic* and ~~strike~~ and `code`

## 9. Horizontal Rule

Before rule

---

After rule

## 10. Code Fences

```text
plain code fence line one
plain code fence line two with symbols * _ ~ ` | and more
0123456789 0123456789 0123456789 0123456789
```

~~~text
tilde fence line one
tilde fence line two with longer content to verify caret stability in raw fence mode
~~~

## 11. Long Mixed Stress

This long mixed line starts plain then switches to **bold text for a short span** and then returns to plain text before going into `inline code that is slightly longer than usual` and then back into plain text again before *italic words appear here* and then more normal text follows toward the far right edge of the document.

Start **bold** middle `code` more **bold again** more plain *italic* more plain ~~strike~~ more plain `code again` final plain ending.

AA AA AA AA AA **BB BB BB** CC CC CC `DD DD DD` EE EE EE *FF FF FF* GG GG GG ~~HH HH HH~~ II II II `JJ JJ JJ` KK KK KK

## 12. Column Ruler

1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
---------1---------2---------3---------4---------5---------6---------7---------8---------9--------10

Use the ruler lines above to compare the visual caret position against expected character boundaries.

## 13. Edge Cases Near Line End

Ends with plain text at the final character

Ends with `code`

Ends with **bold**

Ends with *italic*

Ends with ~~strike~~

| Ends | In | Table |
| --- | --- | --- |
| final | pipe | check |

## 14. Repetition Block

Repeat this line a few times while typing in the middle and near the end:

The quick brown fox jumps over the lazy dog 0123456789 `code` **bold** *italic* ~~strike~~ and then more plain text after all formatted segments.

The quick brown fox jumps over the lazy dog 0123456789 `code` **bold** *italic* ~~strike~~ and then more plain text after all formatted segments.

The quick brown fox jumps over the lazy dog 0123456789 `code` **bold** *italic* ~~strike~~ and then more plain text after all formatted segments.
