// Integration checks use a temporary Git repository; the user's checkout is never mutated.
import { mkdtemp, mkdir, readFile, writeFile, copyFile, unlink } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import assert from 'node:assert/strict';

const source = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const fixture = await mkdtemp(resolve(tmpdir(), 'framework-doc-refresh-'));
const docs = 'docs/documentation';
await mkdir(resolve(fixture, `${docs}/maintenance`), { recursive: true });
await mkdir(resolve(fixture, `${docs}/vendor`), { recursive: true });
await mkdir(resolve(fixture, 'Assets/Scripts'), { recursive: true });
for (const file of ['build.mjs', 'document-map.mjs', 'favicon.svg', 'maintenance/refresh.mjs', 'maintenance/validate.mjs', 'maintenance/script-index.mjs', 'maintenance/csharp-highlight.mjs']) await copyFile(resolve(source, file), resolve(fixture, docs, file));
for (const [file, content] of Object.entries({
  '00-start.md': '# Fixture introduction\n',
  '01-project.md': '# Fixture structure\n',
  '07-diagrams.md': '# Fixture diagrams\n\n```mermaid\nflowchart LR\n A --> B\n```\n',
  '20-outgame-diagrams.md': '# 아웃게임 다이어그램 · 예정\n',
  '22-character-diagrams.md': '# Fixture character\n\n```mermaid\nflowchart LR\n A --> B\n```\n',
  '30-document-map.md': '# Fixture relationship map\n\n## 문서 연결\n\n- [AGENTS](../../AGENTS.md) — Project instructions\n  - [소개](00-start.md) — 시작\n  - [구조](01-project.md) — 구조\n  - [인게임](07-diagrams.md) — 흐름\n    - [아웃게임](20-outgame-diagrams.md) — 예정\n    - [캐릭터](22-character-diagrams.md) — 구성\n  - [이력](11-update-history.md) — 검토\n',
  '11-update-history.md': '# Fixture history\n',
  'vendor/mermaid.min.js': '// Rendering is outside the scope of this integration test.\n',
})) await writeFile(resolve(fixture, docs, file), content);
await writeFile(resolve(fixture, 'Assets/Scripts/Example.cs'), 'class Example {}\n');
await writeFile(resolve(fixture, 'README.md'), '# Fixture\n');
await writeFile(resolve(fixture, 'AGENTS.md'), '# Fixture instructions\n');
function run(executable, args, succeeds = true) {
  const result = spawnSync(executable, args, { cwd: fixture, encoding: 'utf8' });
  if (result.error) throw result.error;
  if (succeeds) assert.equal(result.status, 0, result.stderr || result.stdout);
  else assert.notEqual(result.status, 0, 'Expected command to reject invalid baseline or stale output');
  return result.stdout;
}
const node = (...args) => run(process.execPath, args);
const status = () => JSON.parse(node(`${docs}/maintenance/refresh.mjs`, 'status'));
const build = () => node(`${docs}/build.mjs`);
const recordArgs = mode => [`${docs}/maintenance/refresh.mjs`, 'record', '--mode', mode, '--reviewed', '--note', 'integration fixture review', '--unverified', 'runtime'];
run('git', ['init', '-q']);
run('git', ['add', '.']);
run('git', ['-c', 'user.name=Document Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-qm', 'initial fixture']);
assert.equal(status().fullRefreshRequired, true);
build();
node(...recordArgs('full'));
build();
assert.deepEqual(status().snapshotChanges, []);

// Changing a source without committing must be detected at an unchanged HEAD.
await writeFile(resolve(fixture, 'Assets/Scripts/Example.cs'), 'class Example { int value; }\n');
await writeFile(resolve(fixture, 'Assets/Scripts/NewExample.cs'), 'class NewExample {}\n');
let changes = status();
assert.equal(changes.currentRevision, changes.previousRevision);
assert(changes.snapshotChanges.some(c => c.path.endsWith('/Example.cs') && c.change === 'modified'));
assert(changes.snapshotChanges.some(c => c.path.endsWith('/NewExample.cs') && c.change === 'added'));

// Reject a record if the generated HTML predates edited Markdown.
await writeFile(resolve(fixture, docs, '01-project.md'), '# Changed fixture structure\n');
run(process.execPath, recordArgs('incremental'), false);
build();
node(...recordArgs('incremental'));
build();
assert.deepEqual(status().snapshotChanges, []);

// Committing previously reviewed dirty files must not require a duplicate content review.
run('git', ['add', 'Assets/Scripts']);
run('git', ['-c', 'user.name=Document Fixture', '-c', 'user.email=fixture@example.invalid', 'commit', '-qm', 'reviewed source changes']);
changes = status();
assert(changes.committedChanges.includes('Assets/Scripts/Example.cs'));
assert.deepEqual(changes.snapshotChanges, []);
await unlink(resolve(fixture, 'Assets/Scripts/NewExample.cs'));
assert(status().snapshotChanges.some(c => c.path.endsWith('/NewExample.cs') && c.change === 'deleted'));

// An invalid recorded SHA requires a new full baseline and cannot be advanced incrementally.
const stateFile = resolve(fixture, docs, 'maintenance/update-state.json');
const state = JSON.parse(await readFile(stateFile, 'utf8'));
state.revision = '0'.repeat(40);
await writeFile(stateFile, JSON.stringify(state));
assert.equal(status().fullRefreshRequired, true);
run(process.execPath, recordArgs('incremental'), false);
console.log('PASS: first full baseline, same-HEAD modification/addition/deletion, stale-output rejection, reviewed dirty commit deduplication, invalid-revision fallback.');
console.log(`Disposable fixture retained for inspection: ${fixture}`);
