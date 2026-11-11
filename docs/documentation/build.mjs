import { readdir, readFile, writeFile, access } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { collectScripts } from './maintenance/script-index.mjs';
import { highlightCSharp } from './maintenance/csharp-highlight.mjs';

const root = dirname(fileURLToPath(import.meta.url));
const builtAt = new Date();
const builtAtIso = builtAt.toISOString();
const builtAtKst = new Intl.DateTimeFormat('sv-SE', {
  timeZone: 'Asia/Seoul', year: 'numeric', month: '2-digit', day: '2-digit',
  hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23',
}).format(builtAt) + ' KST (UTC+09:00)';
const files = (await readdir(root)).filter(f => f.endsWith('.md')).sort();
const escape = s => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const ids = new Map(files.map((f, i) => [f, `chapter-${i}`]));
const pageFor = file => file === '00-start.md' ? 'index.html' : file.replace(/\.md$/, '.html');
let links = 0;
async function checkLinks(source, file) {
  // Ignore fenced examples: their links are sample text, not document navigation.
  const prose = source.replace(/```[^\n]*\n[\s\S]*?```/g, '');
  for (const match of prose.matchAll(/\[[^\]]+\]\(([^)]+)\)/g)) {
    const href = match[1];
    if (/^(https?:|#)/.test(href)) continue;
    if (/^[a-z]+:/i.test(href)) throw new Error(`Unsupported link in ${file}: ${href}`);
    await access(resolve(root, href.split('#')[0]));
    links++;
  }
}
function inline(text) {
  const tokens = [];
  const token = html => `\u0000${tokens.push(html) - 1}\u0000`;
  text = text.replace(/`([^`]+)`/g, (_, code) => token(`<code>${escape(code)}</code>`));
  text = text.replace(/!\[([^\]]*)\]\(([^)]+)\)/g, (_, alt, src) => token(`<img class="document-image" src="${escape(src)}" alt="${escape(alt)}">`));
  text = text.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_, label, href) => {
    const target = ids.has(href) ? pageFor(href) : href;
    return token(`<a href="${escape(target)}">${escape(label)}</a>`);
  });
  return escape(text).replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>').replace(/\u0000(\d+)\u0000/g, (_, i) => tokens[Number(i)]);
}
function render(source, id) {
  const lines = source.replaceAll('\r', '').split('\n');
  const html = [];
  let i = 0, heading = 0;
  const special = line => /^(#{1,6} |```|\| |[-*] |\d+\. |> |---\s*$)/.test(line);
  while (i < lines.length) {
    const line = lines[i];
    if (!line.trim()) { i++; continue; }
    if (line.startsWith('```')) {
      const lang = line.slice(3).trim(); const code = []; i++;
      while (i < lines.length && !lines[i].startsWith('```')) code.push(lines[i++]);
      if (i === lines.length) throw new Error(`Unclosed code fence: ${id}`);
      i++;
      const source = escape(code.join('\n'));
      if (lang === 'mermaid') {
        html.push(`<figure class="diagram"><div class="mermaid">${source}</div><p class="diagram-status" role="status">다이어그램을 준비하고 있습니다.</p><details><summary>Mermaid 원문 보기</summary><pre><code>${source}</code></pre></details></figure>`);
      } else {
        html.push(`<div class="code-label">${escape(lang || 'text')}</div><pre${['csharp', 'cs'].includes(lang) ? ' class="csharp-code"' : ''}><code>${['csharp', 'cs'].includes(lang) ? highlightCSharp(code.join('\n')).join('\n') : source}</code></pre>`);
      }
      continue;
    }
    const h = /^(#{1,6}) (.+)$/.exec(line);
    if (h) { html.push(`<h${h[1].length} id="${id}-h${heading++}">${inline(h[2])}</h${h[1].length}>`); i++; continue; }
    if (/^---\s*$/.test(line)) { html.push('<hr>'); i++; continue; }
    if (line.startsWith('|')) {
      const rows = [];
      while (i < lines.length && lines[i].startsWith('|')) rows.push(lines[i++].split('|').slice(1, -1).map(s => s.trim()));
      const head = rows.shift();
      if (rows[0]?.every(cell => /^:?-+:?$/.test(cell))) rows.shift();
      html.push(`<div class="table-wrap"><table><thead><tr>${head.map(c => `<th>${inline(c)}</th>`).join('')}</tr></thead><tbody>${rows.map(row => `<tr>${row.map(c => `<td>${inline(c)}</td>`).join('')}</tr>`).join('')}</tbody></table></div>`); continue;
    }
    if (/^([-*] |\d+\. )/.test(line)) {
      const ordered = /^\d/.test(line); const pattern = ordered ? /^\d+\. / : /^[-*] /; const items = [];
      while (i < lines.length && pattern.test(lines[i])) {
        const item = [lines[i++].replace(pattern, '').replace(/\\$/, '')];
        const nested = [];
        while (i < lines.length && /^\s+\S/.test(lines[i])) {
          if (/^\s+[-*] /.test(lines[i])) nested.push(lines[i++].trim().slice(2));
          else item.push(lines[i++].trim());
        }
        items.push(`<li>${inline(item.join(' '))}${nested.length ? `<ul>${nested.map(s => `<li>${inline(s)}</li>`).join('')}</ul>` : ''}</li>`);
      }
      html.push(`<${ordered ? 'ol' : 'ul'}>${items.join('')}</${ordered ? 'ol' : 'ul'}>`); continue;
    }
    if (line.startsWith('> ')) { html.push(`<blockquote>${inline(line.slice(2))}</blockquote>`); i++; continue; }
    const paragraph = [line]; i++;
    while (i < lines.length && lines[i].trim() && !special(lines[i])) paragraph.push(lines[i++]);
    html.push(`<p>${inline(paragraph.join(' '))}</p>`);
  }
  return html.join('\n');
}
const chapters = [];
for (const file of files) {
  const source = await readFile(resolve(root, file), 'utf8');
  await checkLinks(source, file);
  const title = /^# (.+)$/m.exec(source)?.[1] || file;
  chapters.push({ file, title, id: ids.get(file), sourceHash: createHash('sha256').update(source).digest('hex'), html: render(source, ids.get(file)) });
}
if (!chapters.length) throw new Error('No Markdown documents found');
const scripts = await collectScripts(root);
scripts.forEach(script => { script.highlighted = highlightCSharp(script.source); });
const buildHash = createHash('sha256').update(JSON.stringify(chapters.map(c => [c.file, c.sourceHash]))).update(JSON.stringify(scripts.map(s => [s.path, s.hash]))).update(await readFile(fileURLToPath(import.meta.url))).update(await readFile(resolve(root, 'maintenance/csharp-highlight.mjs'))).digest('hex');
const groups = [
  ['프로젝트 구조', ['00', '01', '09']],
  ['다이어그램', ['21', '07', '22', '20']],
  ['개발된 기능', ['08', '23', '24', '26', '27', '25', '28']],
  ['작성 요령', ['12', '13', '18', '03', '02', '17', '19', '14', '15', '16', '04', '05', '06', '10', '11']],
];
const categoryLabel = title => `${groups.findIndex(([name]) => name === title) + 1}. ${title}`;
const grouped = new Set(groups.flatMap(([, prefixes]) => prefixes));
const extra = chapters.filter(c => !grouped.has(c.file.slice(0, 2)));
if (extra.length) groups.push(['추가 문서', extra.map(c => c.file.slice(0, 2))]);
const ordered = groups.flatMap(([, prefixes]) => prefixes.flatMap(prefix => chapters.filter(c => c.file.slice(0, 2) === prefix)));
const labels = { '00': '프로젝트 소개', '01': '폴더와 시스템 구조', '08': '구현 기능과 소스', '09': '환경과 라이선스', '07': '전체 연결', '02': '네이밍 컨벤션', '03': 'Function · 함수 작성', '04': 'Unity 작업 기본 규칙', '05': '예제 찾아보기 · 문서 양식', '06': '작업과 검증 절차', '10': '다큐먼트 갱신 매뉴얼', '11': '갱신 리비전과 이력', '12': 'Class · 클래스 작성', '13': 'Struct · 구조체 작성', '14': 'Buff 만들기', '15': 'Skill 만들기', '16': 'HUD 만들기', '17': '주석·줄바꿈 규칙', '18': 'Enum · 열거형 작성', '19': '이벤트 작성', '20': '예정', '21': '예정', '22': '캐릭터', '23': '캐릭터 · 플레이어 제어', '24': 'HUD', '25': '유틸리티 · 에디터' };
const writingSubgroups = [
  ['기본 작성', ['12', '13', '18', '03', '02', '17', '19']],
  ['기능별 작성', ['14', '15', '16']],
  ['Unity와 문서 관리', ['04', '05', '06', '10', '11']],
];
const subgroupFor = prefix => writingSubgroups.find(([, prefixes]) => prefixes.includes(prefix))?.[0];
const diagramSubgroupFor = prefix => ({ '21': '전체 프로세스', '07': '인게임', '22': '인게임', '20': '아웃게임' })[prefix];
const searchIndex = Object.fromEntries(chapters.map(c => [c.id, c.html.replace(/<[^>]*>/g, ' ').toLocaleLowerCase()]));
const views = ordered.map((c, index) => {
  const category = groups.find(([, prefixes]) => prefixes.includes(c.file.slice(0, 2)))[0];
  const label = labels[c.file.slice(0, 2)] || c.title;
  const prev = ordered[index - 1], next = ordered[index + 1];
  return {
    id: c.id, page: pageFor(c.file), title: c.title,
    breadcrumb: `프로젝트 다큐먼트 / ${categoryLabel(category)} / ${category === '다이어그램' ? diagramSubgroupFor(c.file.slice(0, 2)) + ' / ' : ''}${category === '작성 요령' ? (subgroupFor(c.file.slice(0, 2)) || '') + ' / ' : ''}${label}`,
    content: `${c.html}<div class="source">문서 원본 · <a href="${escape(c.file)}">${escape(c.file)}</a></div>`,
    pager: `${prev ? `<a href="${pageFor(prev.file)}"><small>이전 문서</small>${escape(labels[prev.file.slice(0, 2)] || prev.title)}</a>` : '<span></span>'}${next ? `<a href="${pageFor(next.file)}"><small>다음 문서</small>${escape(labels[next.file.slice(0, 2)] || next.title)} →</a>` : ''}`,
  };
});
for (const [pageIndex, current] of ordered.entries()) {
const category = groups.find(([, prefixes]) => prefixes.includes(current.file.slice(0, 2)))[0];
const nav = groups.map(([title, prefixes]) => {
  let previousSubgroup;
  let folded = false;
  return `<section class="nav-section"><h2 class="nav-group">${escape(categoryLabel(title))}</h2>` + prefixes.flatMap(prefix => chapters.filter(c => c.file.slice(0, 2) === prefix)).map(c => {
    const prefix = c.file.slice(0, 2);
    const subgroup = title === '작성 요령' ? subgroupFor(prefix) : title === '다이어그램' ? diagramSubgroupFor(prefix) : undefined;
    const heading = subgroup && subgroup !== previousSubgroup
      ? subgroup === 'Unity와 문서 관리'
        ? '<details class="nav-fold"><summary>Unity와 문서 관리</summary>'
        : `<h3 class="nav-subgroup">${escape(subgroup)}</h3>`
      : '';
    if (subgroup === 'Unity와 문서 관리') folded = true;
    previousSubgroup = subgroup;
    return heading + `<a href="${pageFor(c.file)}" data-chapter="${c.id}"${c === current ? ' aria-current="page"' : ''}>${escape(labels[prefix] || c.title)}</a>`;
  }).join('\n') + (folded ? '</details>' : '') + '</section>';
}).join('\n');
const content = `<article id="${current.id}">${current.html}<div class="source">문서 원본 · <a href="${escape(current.file)}">${escape(current.file)}</a></div></article>`;
const prev = ordered[pageIndex - 1], next = ordered[pageIndex + 1];
const pager = `<nav class="pager" aria-label="이전 다음 문서">${prev ? `<a href="${pageFor(prev.file)}"><small>이전 문서</small>${escape(labels[prev.file.slice(0, 2)] || prev.title)}</a>` : '<span></span>'}${next ? `<a href="${pageFor(next.file)}"><small>다음 문서</small>${escape(labels[next.file.slice(0, 2)] || next.title)} →</a>` : ''}</nav>`;
await writeFile(resolve(root, pageFor(current.file)), `<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><link rel="icon" type="image/svg+xml" href="favicon.svg"><meta name="documentation-build-hash" content="${buildHash}"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${escape(current.title)} · Unity 샘플 프로젝트</title>
<style>html{scrollbar-width:thin;scrollbar-color:#94a7c2 #edf2f8}aside{scrollbar-width:thin;scrollbar-color:#526c90 #14233b}pre,.table-wrap,.diagram{scrollbar-width:thin;scrollbar-color:#94a7c2 transparent}::-webkit-scrollbar{width:9px;height:9px}::-webkit-scrollbar-track{background:#edf2f8}::-webkit-scrollbar-thumb{background:#94a7c2;border:2px solid #edf2f8;border-radius:8px}::-webkit-scrollbar-thumb:hover{background:#647fa5}::-webkit-scrollbar-corner{background:transparent}aside::-webkit-scrollbar-track{background:#14233b}aside::-webkit-scrollbar-thumb{background:#526c90;border-color:#14233b}aside::-webkit-scrollbar-thumb:hover{background:#7896bd}pre::-webkit-scrollbar-track{background:#17263c}pre::-webkit-scrollbar-thumb{background:#526c90;border-color:#17263c}.table-wrap::-webkit-scrollbar-track,.diagram::-webkit-scrollbar-track{background:transparent}.table-wrap::-webkit-scrollbar-thumb,.diagram::-webkit-scrollbar-thumb{border-color:#fff}.nav-fold{margin-top:16px}.nav-fold>summary{color:#9db4d5;font-size:12px;padding:8px 10px;border-radius:6px;cursor:pointer}.nav-fold>summary:hover{background:#24354e;color:#fff}.nav-fold[open]>summary{margin-bottom:5px}.nav-subgroup{font-size:11px;color:#9db4d5;letter-spacing:.04em;margin:16px 12px 5px;font-weight:600}.nav-section{margin:26px 0}.nav-group{color:#fff;font-size:15px;font-weight:750;margin:0 0 8px;padding:0;border:0;letter-spacing:.03em}.nav-section a{margin-left:6px;padding:7px 12px;border-left:2px solid #384a65;border-radius:0 6px 6px 0}nav a[aria-current="page"]{background:#2b4162;color:#fff;border-left-color:#8eb4ff;font-weight:650}.pager{display:flex;justify-content:space-between;gap:16px}.pager a{display:block;background:#fff;border:1px solid var(--line);border-radius:10px;padding:14px 20px;color:var(--ink);min-width:35%;font-size:15px}.pager a:hover,.pager a:focus{background:#edf2fa;color:var(--accent)}.pager small{display:block;color:var(--muted);font-size:11px}.breadcrumb{color:var(--muted);font-size:13px;margin-bottom:20px}.source{margin-top:36px;padding-top:16px;border-top:1px solid var(--line)}.diagram{margin:24px 0;padding:16px;border:1px solid var(--line);border-radius:10px;overflow:auto}.mermaid{min-width:480px;text-align:center}.mermaid svg{height:auto;max-width:100%}.diagram-status{font-size:13px;color:var(--muted)}details summary{cursor:pointer;color:var(--accent);padding:12px 0}
:root{color-scheme:light;--ink:#233047;--muted:#59677c;--line:#dce3ec;--accent:#245bc4}*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:24px}body{margin:0;background:#f5f7fb;color:var(--ink);font:16px/1.8 system-ui,-apple-system,"Segoe UI",sans-serif}aside{position:fixed;width:290px;inset:0 auto 0 0;background:#14233b;color:#fff;padding:34px 22px;overflow:auto}.brand{font-size:21px;font-weight:750;line-height:1.4}.tagline{color:#b6c4d9;font-size:13px;margin:12px 0 26px}label{display:block;font-size:13px;color:#b6c4d9}input{width:100%;margin:8px 0 22px;padding:12px;border:1px solid #526078;border-radius:8px;background:#24354e;color:white;font:inherit}nav a{display:flex;gap:12px;color:#d4dfed;text-decoration:none;padding:12px 9px;border-radius:7px;font-size:14px}nav a:hover,nav a:focus{background:#2b4162;color:white}nav span{color:#89aaf2;font:12px/2.2 monospace}.hint{font-size:12px;color:#b6c4d9;margin-top:28px}main{margin-left:290px;max-width:1240px;padding:48px 56px}.eyebrow{color:var(--accent);letter-spacing:2px;font-size:12px;font-weight:700}.hero h1{font-size:40px;line-height:1.3;margin:12px 0}.hero p{color:var(--muted)}.hero{margin-bottom:36px}article{background:white;border:1px solid var(--line);border-radius:12px;padding:36px 42px;margin:24px 0;scroll-margin-top:24px}.source{font-size:11px;letter-spacing:1px;color:var(--muted)}h1{font-size:28px;line-height:1.4}h2{font-size:21px;margin-top:34px;border-top:1px solid var(--line);padding-top:24px}h3{font-size:18px}a{color:var(--accent);text-underline-offset:3px}p,li{overflow-wrap:anywhere}li{margin:7px 0}code{font:13px/1.7 Consolas,monospace;background:#eef2f8;border-radius:4px;padding:2px 5px}pre{background:#17263c;color:#e3edff;padding:20px;border-radius:0 0 8px 8px;overflow:auto;margin-top:0}pre code{background:none;padding:0;white-space:pre}.code-label{background:#243954;color:#b6cef3;font:11px/1.8 monospace;padding:6px 20px;border-radius:8px 8px 0 0}.table-wrap{overflow:auto}table{border-collapse:collapse;width:100%;font-size:14px}th,td{padding:12px 14px;border-bottom:1px solid var(--line);text-align:left}th{background:#edf2fa}blockquote{border-left:3px solid var(--accent);margin:20px 0;padding:8px 20px;background:#f4f7fc}[hidden]{display:none!important}#empty{padding:30px;color:var(--muted)}footer{color:var(--muted);font-size:13px;margin-top:30px}@media(max-width:850px){aside{position:static;width:auto;padding:24px}nav[aria-label="문서 목차"]{display:block}.nav-section{margin:18px 0}.hint{margin-top:12px}main{margin:0;padding:24px 16px}article{padding:24px 20px}.hero h1{font-size:30px}}@media print{aside,.hero,footer,.source{display:none}main{margin:0;padding:0;max-width:none}article{border:none;padding:0;break-before:page}.pager{display:none}pre{white-space:pre-wrap}a{color:inherit}}
#script-results{margin:4px 0 24px}#script-results a{display:block;padding:10px;margin:6px 0;background:#24354e;border-radius:7px;color:#d4dfed;text-decoration:none}#script-results a:hover{background:#2b4162}#script-results strong{font-size:14px}#script-results small{display:block;font-size:11px;overflow-wrap:anywhere;color:#b6c4d9}.search-snippet{max-height:3.6em;overflow:hidden}.script-path{font-size:13px;color:var(--muted)}.script-code{padding:16px 0}.code-row{display:flex;min-width:max-content;line-height:1.7;padding-right:20px}.line-number{display:inline-block;flex:none;width:64px;padding-right:16px;text-align:right;color:#8399b9;user-select:none}.code-row.matched{background:#30466a}.csharp-code,.script-code{background:#1e1e1e;color:#d4d4d4;font:14px/1.7 Consolas,"Cascadia Mono",monospace;tab-size:4}.csharp-code code,.script-code code{font:inherit;color:inherit}.code-label{background:#252526;color:#cccccc}.line-number{color:#858585}.code-row.matched{background:#3d3d20}.cs-keyword,.cs-directive{color:#569cd6}.cs-control{color:#c586c0}.cs-type{color:#4ec9b0}.cs-method{color:#dcdcaa}.cs-string{color:#ce9178}.cs-comment{color:#6a9955}.cs-number{color:#b5cea8}.cs-identifier{color:#9cdcfe}.csharp-code::-webkit-scrollbar-track,.script-code::-webkit-scrollbar-track{background:#1e1e1e}.csharp-code::-webkit-scrollbar-thumb,.script-code::-webkit-scrollbar-thumb{background:#686868;border-color:#1e1e1e}.csharp-code,.script-code{scrollbar-color:#686868 #1e1e1e}.search-match{font-weight:inherit;color:inherit;text-decoration:underline;text-underline-offset:3px;text-decoration-thickness:1px}.brand{display:block;color:#fff;text-decoration:none}.brand:hover,.brand:focus-visible{color:#b9d3ef}.document-image{display:block;width:100%;height:auto;border-radius:8px}.build-info{text-align:right;color:var(--muted);font-size:12px;margin-bottom:12px}.build-info time{font-variant-numeric:tabular-nums}</style></head><body><aside><a class="brand" href="index.html" aria-label="프로젝트 소개 홈">Unity<br>샘플 프로젝트</a><p class="tagline">프로젝트 다큐먼트 · 사람과 에이전트의 공통 기준</p><label for="search">문서 · 스크립트 검색</label><input id="search" type="search" placeholder="예: 네이밍, Awake, 스킬"><section id="script-results" hidden aria-label="스크립트 검색 결과"></section><nav aria-label="문서 목차">${nav}</nav><p class="hint">Markdown을 수정한 뒤<br>node docs/documentation/build.mjs<br>명령으로 이 페이지를 갱신합니다.</p></aside><main><div class="build-info">마지막 갱신 · <time datetime="${builtAtIso}">${builtAtKst}</time></div><div class="breadcrumb">프로젝트 다큐먼트 / ${escape(categoryLabel(category))} / ${category === '다이어그램' ? escape(diagramSubgroupFor(current.file.slice(0, 2))) + ' / ' : ''}${category === '작성 요령' ? escape(subgroupFor(current.file.slice(0, 2)) || '') + ' / ' : ''}${escape(labels[current.file.slice(0, 2)] || current.title)}</div><div id="empty" hidden role="status">검색 결과가 없습니다.</div>${content}${pager}<footer>원본: docs/documentation/*.md · 인터넷 연결 없이 열람 가능 · HTML은 자동 생성됩니다.</footer></main><script>
const input = document.getElementById('search');
const scripts = ${JSON.stringify(scripts).replaceAll('<', '\\u003c')};
const safe = text => text.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
function searchSelection(plain, terms) {
  const lower = plain.toLocaleLowerCase();
  const selected = Array(plain.length).fill(false);
  for (const term of terms) {
    if (!term) continue;
    for (let start = lower.indexOf(term); start >= 0; start = lower.indexOf(term, start + 1)) {
      selected.fill(true, start, start + term.length);
    }
    if (lower.includes(term) || !/^[a-z_][a-z0-9_]*$/i.test(term)) continue;
    for (const identifier of plain.matchAll(/[a-z_][a-z0-9_]*/gi)) {
      const positions = [];
      let position = 0;
      for (const character of term) {
        const found = identifier[0].toLocaleLowerCase().indexOf(character, position);
        if (found < 0) break;
        positions.push(identifier.index + found);
        position = found + 1;
      }
      if (positions.length === term.length) positions.forEach(index => { selected[index] = true; });
    }
  }
  return selected;
}
function underlineMatches(html, terms) {
  const parts = html.split(/(<[^>]+>)/g);
  const decode = text => text.replaceAll('&quot;', '"').replaceAll('&gt;', '>').replaceAll('&lt;', '<').replaceAll('&amp;', '&');
  const plain = parts.filter(part => !part.startsWith('<')).map(decode).join('');
  const selected = searchSelection(plain, terms);
  let offset = 0;
  return parts.map(part => {
    if (part.startsWith('<')) return part;
    const text = decode(part);
    let result = '', underlined = false;
    for (let index = 0; index < text.length; index++) {
      if (selected[offset + index] !== underlined) {
        result += selected[offset + index] ? '<u class="search-match">' : '</u>';
        underlined = selected[offset + index];
      }
      result += safe(text[index]);
    }
    offset += text.length;
    return result + (underlined ? '</u>' : '');
  }).join('');
}
function scrollToDocument() {
  const isMobile = window.matchMedia('(max-width: 850px)').matches;
  const article = document.querySelector('article');
  const top = isMobile ? Math.max(0, window.scrollY + article.getBoundingClientRect().top - 24) : 0;
  window.scrollTo({ top, behavior: 'instant' });
}
function showScript(id, preserveScroll = false) {
  const script = scripts.find(script => script.id === id);
  if (!script) return false;
  activeId = id;
  const article = document.querySelector('article');
  article.id = 'script-view';
  const terms = input.value.toLocaleLowerCase().trim().split(/\\s+/).filter(Boolean);
  article.innerHTML = '<h1>' + safe(script.name) + '</h1><p class="script-path">' + safe(script.path) + '</p><div class="code-label">C# · 읽기 전용 · 문서 빌드 시점의 소스</div><pre class="script-code"><code>' + script.source.replaceAll('\\r', '').split('\\n').map((line, index) => '<span class="code-row' + (searchSelection(line, terms).some(Boolean) ? ' matched' : '') + '"><span class="line-number">' + (index + 1) + '</span><span>' + underlineMatches(script.highlighted[index], terms) + '</span></span>').join('') + '</code></pre>';
  document.querySelector('.breadcrumb').textContent = '프로젝트 다큐먼트 / 스크립트 / ' + script.path;
  document.querySelector('.pager').innerHTML = '';
  document.querySelectorAll('[data-chapter]').forEach(link => link.removeAttribute('aria-current'));
  document.title = script.name + ' · Unity 샘플 프로젝트';
  if (!preserveScroll) scrollToDocument();
  return true;
}
const views = ${JSON.stringify(views).replaceAll('<', '\\u003c')};
const initialId = ${JSON.stringify(current.id)};
let activeId = initialId;
function showChapter(id) {
  if (showScript(id)) return;
  const view = views.find(view => view.id === id);
  if (!view) return;
  if (activeId === id) {
    scrollToDocument();
    return;
  }
  activeId = id;
  const article = document.querySelector('article');
  article.id = view.id;
  article.innerHTML = view.content;
  document.querySelector('.breadcrumb').textContent = view.breadcrumb;
  document.querySelector('.pager').innerHTML = view.pager;
  document.title = view.title + ' · Unity 샘플 프로젝트';
  document.querySelectorAll('[data-chapter]').forEach(link => {
    if (link.dataset.chapter === id) link.setAttribute('aria-current', 'page');
    else link.removeAttribute('aria-current');
  });
  scrollToDocument();
  if (window.mermaid) renderDiagrams();
}
document.addEventListener('click', event => {
  if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
  const link = event.target.closest('a');
  if (!link || link.target || link.hasAttribute('download')) return;
  const scriptId = (link.getAttribute('href') || '').slice(1);
  if (scripts.some(script => script.id === scriptId)) {
    event.preventDefault();
    showScript(scriptId);
    if (location.hash !== '#' + scriptId) history.pushState(null, '', '#' + scriptId);
    return;
  }
  const view = views.find(view => view.page === link.getAttribute('href'));
  if (!view) return;
  event.preventDefault();
  if (view.page === 'index.html') {
    input.value = '';
    updateSearch();
  }
  showChapter(view.id);
  if (location.hash !== '#' + view.id) history.pushState(null, '', '#' + view.id);
});
window.addEventListener('hashchange', () => showChapter(location.hash.slice(1) || initialId));
window.addEventListener('popstate', () => showChapter(location.hash.slice(1) || initialId));
history.scrollRestoration = 'manual';
showChapter(location.hash.slice(1));
const searchIndex = ${JSON.stringify(searchIndex).replaceAll('<', '\\u003c')};
function matchesScriptName(name, term) {
  const identifier = name.replace(/\\.cs$/i, '').toLocaleLowerCase();
  let position = 0;
  for (const character of term) {
    const found = identifier.indexOf(character, position);
    if (found < 0) return false;
    position = found + 1;
  }
  return true;
}
function updateSearch() {
  const terms = input.value.toLocaleLowerCase().trim().split(/\\s+/).filter(Boolean);
  const results = document.getElementById('script-results');
  const matches = terms.length ? scripts.filter(script => terms.every(term => (script.path + '\\n' + script.source).toLocaleLowerCase().includes(term) || matchesScriptName(script.name, term))) : [];
  matches.sort((a, b) => Number(terms.every(term => b.name.toLocaleLowerCase().includes(term))) - Number(terms.every(term => a.name.toLocaleLowerCase().includes(term))));
  results.hidden = !terms.length;
  results.innerHTML = '<h2 class="nav-group">스크립트 (' + matches.length + ')</h2>' + matches.map(script => {
    const lines = script.source.replaceAll('\\r', '').split('\\n');
    const index = lines.findIndex(line => searchSelection(line, terms).some(Boolean));
    return '<a href="#' + script.id + '"><strong>' + safe(script.name) + '</strong><small>' + underlineMatches(safe(script.path), terms) + '</small>' + (index >= 0 ? '<small class="search-snippet">L' + (index + 1) + ': ' + underlineMatches(safe(lines[index].trim()), terms) + '</small>' : '') + '</a>';
  }).join('');
  let count = 0;
  document.querySelectorAll('[data-chapter]').forEach(link => {
    const text = searchIndex[link.dataset.chapter];
    const visible = terms.every(term => text.includes(term));
    link.hidden = !visible;
    if (visible) count++;
  });
  document.querySelectorAll('.nav-section').forEach(section => {
    section.hidden = ![...section.querySelectorAll('[data-chapter]')].some(link => !link.hidden);
  });
  document.querySelectorAll('.nav-fold').forEach(fold => {
    fold.hidden = ![...fold.querySelectorAll('[data-chapter]')].some(link => !link.hidden);
    fold.open = terms.length > 0;
  });
  document.querySelectorAll('.nav-subgroup').forEach(heading => {
    let visible = false;
    let sibling = heading.nextElementSibling;
    while (sibling && sibling.tagName === 'A') {
      visible ||= !sibling.hidden;
      sibling = sibling.nextElementSibling;
    }
    heading.hidden = !visible;
  });
  document.getElementById('empty').hidden = count + matches.length > 0;
  if (scripts.some(script => script.id === activeId)) {
    const code = document.querySelector('.script-code');
    const scrollLeft = code ? code.scrollLeft : 0;
    showScript(activeId, true);
    if (code) document.querySelector('.script-code').scrollLeft = scrollLeft;
  }
}
input.addEventListener('input', updateSearch);
</script><script src="vendor/mermaid.min.js"></script><script>
async function renderDiagrams() {
  if (!window.mermaid) {
    document.querySelectorAll('.diagram-status').forEach(status => { status.textContent = 'Mermaid를 불러오지 못했습니다. vendor 폴더를 확인하거나 원문을 펼쳐 보세요.'; });
    return;
  }
  mermaid.initialize({ startOnLoad: false, securityLevel: 'strict', suppressErrorRendering: true });
  for (const node of document.querySelectorAll('.mermaid')) {
    const status = node.parentElement.querySelector('.diagram-status');
    try {
      await mermaid.run({ nodes: [node] });
      status.hidden = true;
    } catch (error) {
      status.textContent = '다이어그램을 표시하지 못했습니다. Mermaid 원문을 확인하세요.';
      console.error(error);
    }
  }
}
renderDiagrams();
</script></body></html>\n`, 'utf8');
}
console.log(`Generated ${ordered.length} HTML pages; ${links} local links verified.`);

