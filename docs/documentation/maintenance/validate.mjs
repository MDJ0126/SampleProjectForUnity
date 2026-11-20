import { readdir, readFile, access } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Script } from 'node:vm';
import { createHash } from 'node:crypto';
import { collectScripts } from './script-index.mjs';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const entries = await readdir(root);
const markdown = entries.filter(file => file.endsWith('.md')).sort();
const sources = [];
for (const file of markdown) sources.push([file, createHash('sha256').update(await readFile(resolve(root, file), 'utf8')).digest('hex')]);
const scripts = await collectScripts(root);
const expectedHash = createHash('sha256').update(JSON.stringify(sources)).update(JSON.stringify(scripts.map(s => [s.path, s.hash]))).update(await readFile(resolve(root, 'build.mjs'))).update(await readFile(resolve(root, 'maintenance/csharp-highlight.mjs'))).update(await readFile(resolve(root, 'document-map.mjs'))).digest('hex');
let checkedLinks = 0;
let buildTimestamp;
for (const file of markdown) {
  const page = file === '00-start.md' ? 'index.html' : file.replace(/\.md$/, '.html');
  const source = await readFile(resolve(root, file), 'utf8');
  if (/^\s*\/\/\/\s*<summary>.+<\/summary>/m.test(source)) throw new Error(`One-line C# summary: ${file}`);
  const html = await readFile(resolve(root, page), 'utf8');
  const timestamp = /<time datetime="([^"]+)">([^<]+)<\/time>/.exec(html);
  if (!timestamp || Number.isNaN(Date.parse(timestamp[1]))) throw new Error(`Missing build timestamp: ${page}`);
  const expectedTime = new Intl.DateTimeFormat('sv-SE', {
    timeZone: 'Asia/Seoul', year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23',
  }).format(new Date(timestamp[1])) + ' KST (UTC+09:00)';
  if (timestamp[2] !== expectedTime) throw new Error(`Incorrect Korean build time: ${page}`);
  if (buildTimestamp && buildTimestamp !== timestamp[1]) throw new Error(`Inconsistent build time: ${page}`);
  buildTimestamp = timestamp[1];
  if (!html.includes(`<meta name="documentation-build-hash" content="${expectedHash}">`)) throw new Error(`Stale output: ${page}. Run build.mjs before recording a revision.`);
  if ((html.match(/<article /g) || []).length !== 1) throw new Error(`Expected one document per page: ${page}`);
  if ((html.match(/<a [^>]*aria-current="page"/g) || []).length !== 1) throw new Error(`Current page marker: ${page}`);
  const categories = [...html.matchAll(/<h2 class="nav-group">([^<]+)<\/h2>/g)].map(match => match[1]);
  if (categories.slice(0, 4).join(',') !== '1. 프로젝트 구조,2. 다이어그램,3. 개발된 기능,4. 작성 요령') throw new Error(`Category order: ${page}`);
  for (const match of html.matchAll(/<script>([\s\S]*?)<\/script>/g)) new Script(match[1]);
  // Check actual HTML attributes, excluding embedded script strings and styles.
  const markup = html.replace(/<script\b[^>]*>[\s\S]*?<\/script>/g, '').replace(/<style>[\s\S]*?<\/style>/g, '');
  for (const match of markup.matchAll(/(?:href|src)="([^"#]+)"/g)) {
    const href = match[1].replaceAll('&amp;', '&');
    if (/^[a-z]+:/i.test(href)) continue;
    await access(resolve(root, href.split('#')[0]));
    checkedLinks++;
  }
}
const diagrams = await readFile(resolve(root, '07-diagrams.html'), 'utf8');
if ((diagrams.match(/class="mermaid"/g) || []).length !== 1) throw new Error('Expected one ingame diagram');
const character = await readFile(resolve(root, '22-character-diagrams.html'), 'utf8');
if ((character.match(/class="mermaid"/g) || []).length !== 1) throw new Error('Expected one character diagram');
const outgame = await readFile(resolve(root, '20-outgame-diagrams.html'), 'utf8');
if (/class="mermaid"/.test(outgame) || !outgame.includes('아웃게임 다이어그램 · 예정')) throw new Error('Expected planned outgame page without a diagram');
console.log(`Validated ${markdown.length} independent pages and ${checkedLinks} local links; navigation, script syntax and summary formatting passed. Browser and Unity execution are not checked.`);
