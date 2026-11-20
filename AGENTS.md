# SampleProjectForUnity Instructions

## Start Here

Follow the user's instructions and preserve existing changes. Use this file to select the source documents relevant to the task.

| When to read | Sources |
| --- | --- |
| Finding how features and documents fit together | [Project introduction](docs/documentation/00-start.md) and [document map](docs/documentation/30-document-map.md) |
| Writing or changing C# | [Naming](docs/documentation/02-naming.md), [functions](docs/documentation/03-functions.md), [classes](docs/documentation/12-class.md), [structs](docs/documentation/13-struct.md), [enums](docs/documentation/18-enum.md), and [comments and formatting](docs/documentation/17-comments-format.md) as applicable |
| Changing events | Read [events](docs/documentation/19-events.md) before editing the feature |
| Implementing a buff, a skill, or HUD behavior | [Buffs](docs/documentation/14-buff.md), [skills](docs/documentation/15-skill.md), or [HUD](docs/documentation/16-hud.md) |
| Planning or checking Unity work | [Unity work rules](docs/documentation/04-unity.md) and [work and verification](docs/documentation/06-workflow.md) |
| Updating the documentation | [Refresh procedure](docs/documentation/10-updating.md) and [last recorded revision](docs/documentation/11-update-history.md) |

## Code and Verification

- Prefer Korean commit messages. Preserve code identifiers, product names, and other proper names in their original form.
- Place Unity lifecycle methods such as `Awake`, `OnEnable`, `Start`, `Update`, `LateUpdate`, `OnDisable`, and `OnDestroy` at the top of the methods section. Place custom methods and callbacks such as `OnEquippedWeaponChanged` below them.
- Keep ordinary-length calls on one line. Write an XML `summary` in three lines. Use Korean declarative comments in the “~한다.” style to describe the contract and reasoning.
- Write `if` and `else` as multiline blocks. Always use braces, including for a one-line body. Write `switch` across multiple lines and use braces for every `case` and `default` body. Put braces on their own lines, and write the label, body, and `break` or `return` on separate lines.
- Verify claims using source, configuration, and execution. Mark unverified items as `미확인`. Remove temporary generation, configuration, and verification scripts and tools when finished, including their Unity `.meta` files and empty temporary folders. Preserve existing files and results that must be kept.

## Documentation

- Compare the changes and their impact since the last recorded SHA and snapshot. If the baseline is absent or invalid, review the full scope. Follow the refresh procedure to review, build, validate, record the new baseline, and rebuild.
- Markdown is the source. Generate HTML from it. Keep standalone pages for subtopics in this order: project structure, diagrams, implemented features, and writing guidance.
- Keep the README's introduction, documentation links, framework structure, and implemented features. Put detailed environments and licensing in the documentation.
- Keep table-of-contents, previous-page, and next-page navigation in place without a page reload, and preserve browser Back. Keep pages to three to five main rules and short examples where practical. Store detailed update history separately.
- Put one item and its version on each environment or version row. Preserve detailed third-party asset lists, versions, and license distinctions in the environment and licensing chapters. Keep Mermaid role descriptions, edge labels, and feature divisions when summarizing diagrams.
- For a request limited to refreshing documentation, avoid unsolicited game-code changes and do not create an automatic commit. Report the results and unverified items.
