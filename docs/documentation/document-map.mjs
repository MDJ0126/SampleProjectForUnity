const escape = text => text.replaceAll('&', '&amp;').replaceAll('<', '&lt;')
  .replaceAll('>', '&gt;').replaceAll('"', '&quot;');

export const documentMapFile = '30-document-map.md';
export const instructionsFile = '../../AGENTS.md';
export const instructionsId = 'agents-preview';
const marker = '\n## Document Tree\n';

export function readDocumentTree(source, files) {
  const normalized = source.replaceAll('\r\n', '\n');
  const end = normalized.indexOf(marker);
  if (end < 0) throw new Error('AGENTS.md requires a Document Tree section.');
  const treeStart = end + marker.length;
  const nextHeading = normalized.indexOf('\n## ', treeStart);
  const treeEnd = nextHeading < 0 ? normalized.length : nextHeading;
  const ancestors = [];
  const seen = new Set();
  const nodes = [];
  for (const line of normalized.slice(treeStart, treeEnd).split('\n').filter(line => line.trim())) {
    const match = /^( *)(?:- )\[([^\]]+)\]\((AGENTS\.md|docs\/documentation\/[\w-]+\.md)\) — (.+)$/.exec(line);
    if (!match || match[1].length % 2) throw new Error('AGENTS.md 관계도 형식: ' + line);
    const [, spaces, label, target, description] = match;
    const file = target === 'AGENTS.md' ? instructionsFile : target.slice('docs/documentation/'.length);
    const depth = spaces.length / 2;
    if ((nodes.length === 0 ? depth !== 0 || file !== instructionsFile : depth === 0 || depth > ancestors.length) ||
        (!files.includes(file) && file !== instructionsFile) || file === documentMapFile || seen.has(file)) {
      throw new Error('AGENTS.md 관계도 연결 중복 또는 누락: ' + file);
    }
    nodes.push({ file, label, description, depth, parent: ancestors[depth - 1] });
    seen.add(file);
    ancestors.length = depth;
    ancestors.push(file);
  }
  const missing = files.filter(file => file !== documentMapFile && !seen.has(file));
  if (missing.length) throw new Error('AGENTS.md 관계도에 연결되지 않은 문서: ' + missing.join(', '));
  return { before: normalized.slice(0, end), after: normalized.slice(treeEnd), nodes };
}

export function renderDocumentTree(nodes, pageFor) {
  const children = new Map();
  for (const node of nodes) {
    if (!children.has(node.parent)) children.set(node.parent, []);
    children.get(node.parent).push(node);
  }
  function list(parent) {
    const items = children.get(parent);
    if (!items) return '';
    return '<ul>' + items.map(node => {
      const label = escape(node.label) + '.md';
      const title = node.file === instructionsFile ? '<strong>' + label + '</strong>' : '<a href="' + escape(pageFor(node.file)) + '">' + label + '</a>';
      return '<li>' + title + ' <small>— ' + escape(node.description) + '</small>' + list(node.file) + '</li>';
    }).join('') + '</ul>';
  }
  return '<section class="doc-map-text-tree" aria-label="Document Tree"><h2>Document Tree</h2>' + list() + '</section>';
}

export function renderDocumentMap(nodes, ids) {
  const rows = [];
  for (const node of nodes) (rows[node.depth] ??= []).push(node);
  const width = Math.max(910, Math.max(...rows.map(row => row.length)) * 133 + 30);
  const cardWidth = 124, cardHeight = 60, step = 111;
  const height = 48 + (rows.length - 1) * step + cardHeight;
  const positions = new Map();
  for (const [depth, row] of rows.entries()) {
    for (const [index, node] of row.entries()) {
      positions.set(node.file, { x: 15 + (width - 30) * (index + 0.5) / row.length, y: 24 + step * depth });
    }
  }
  const paths = nodes.filter(node => node.parent).map(node => {
    const start = positions.get(node.parent), end = positions.get(node.file);
    const y = start.y + cardHeight, target = end.y - 4, middle = (y + target) / 2;
    const type = node.depth === 1 ? 'direct' : 'reference';
    return `<path class="${type}" d="M${start.x},${y} C${start.x},${middle} ${end.x},${middle} ${end.x},${target - 5}"/><polygon class="${type}" points="${end.x},${target} ${end.x - 4},${target - 7} ${end.x + 4},${target - 7}"/>`;
  }).join('');
  const buttons = nodes.map(node => {
    const { x, y } = positions.get(node.file);
    return `<button type="button" class="doc-map-node ${node.depth < 2 ? 'doc-map-primary' : ''}" data-document-preview="${node.file === instructionsFile ? instructionsId : ids.get(node.file)}" style="left:${x - cardWidth / 2}px;top:${y}px" title="${escape(node.file)}"><strong>${escape(node.label)}.md</strong><small>${escape(node.description)}</small></button>`;
  }).join('');
  return `<section class="doc-map" aria-label="AGENTS.md 관계도"><div class="doc-map-legend"><span>주제</span><span>관련 문서</span><span>${nodes.length}개 문서</span></div><div class="doc-map-scroll" tabindex="0" aria-label="AGENTS.md 관계도, 가로 스크롤 가능"><div class="doc-map-board" style="width:${width}px;height:${height}px"><svg aria-hidden="true" viewBox="0 0 ${width} ${height}" width="${width}" height="${height}">${paths}</svg>${buttons}</div></div></section>`;
}

export const documentMapDialog = '<dialog id="doc-map-preview" aria-labelledby="doc-map-title"><header><strong id="doc-map-title"></strong><a class="doc-map-page">본문 페이지 열기</a><form method="dialog"><button>닫기</button></form></header><div class="doc-map-preview-content"></div></dialog>';

export const documentMapStyles = `
main:has(.doc-map){max-width:1660px;padding-inline:clamp(16px,2vw,30px)}
article:has(.doc-map){padding-inline:clamp(14px,1.8vw,24px)}
.doc-map{margin-top:20px;overflow:hidden;border:1px solid #414958;border-radius:12px;background:#171b23;color:#f2f4f9}
.doc-map-legend{display:flex;gap:18px;flex-wrap:wrap;padding:10px 14px;border-bottom:1px solid #353e4b;font-size:12px}
.doc-map-legend span:first-child::before,.doc-map-legend span:nth-child(2)::before{content:'';display:inline-block;vertical-align:middle;width:16px;margin-right:6px;border-top:2px solid #d5bf8c}
.doc-map-legend span:nth-child(2)::before{border-top:2px dashed #a0aec5}
.doc-map-legend span:last-child{margin-left:auto}
.doc-map-scroll{overflow:auto;scrollbar-color:#79869b #181c25}
.doc-map-board{position:relative;margin-inline:auto;background-image:linear-gradient(#242b36 1px,transparent 1px),linear-gradient(90deg,#242b36 1px,transparent 1px);background-size:22px 22px}
.doc-map-board svg{position:absolute;inset:0;pointer-events:none}
.doc-map-board path{fill:none;stroke-width:1.35}
.doc-map-board path.direct{stroke:#d5bf8c}.doc-map-board polygon.direct{fill:#d5bf8c}
.doc-map-board path.reference{stroke:#a0aec5;stroke-dasharray:5 5}.doc-map-board polygon.reference{fill:#a0aec5}
.doc-map-node{position:absolute;display:grid;align-content:center;width:124px;height:60px;padding:6px 8px;text-align:left;word-break:keep-all;font:12px/1.24 system-ui,sans-serif;color:#f1f5fc;background:#222a37;border:1px solid #526078;border-radius:6px;cursor:pointer}
.doc-map-node small{font-size:10.5px;color:#c1cde1;margin-top:4px}
.doc-map-primary{background:#30291e;border-color:#806d45}.doc-map-primary small{color:#e3d2ac}
.doc-map-node:hover,.doc-map-node:focus-visible{outline:2px solid #ffc46e;outline-offset:2px}
#doc-map-preview{width:min(820px,calc(100% - 28px));max-height:84vh;padding:0;border:1px solid #596c8a;border-radius:12px;color:var(--ink);background:white;overflow:hidden}
#doc-map-preview[open]{display:flex;flex-direction:column}
#doc-map-preview::backdrop{background:#0a1021bb}
#doc-map-preview header{display:flex;gap:12px;align-items:center;flex-wrap:wrap;padding:12px 18px;background:#eaf0f8;border-bottom:1px solid #cad7e7}
#doc-map-title{margin-right:auto}
#doc-map-preview button{padding:5px 10px;background:white;color:var(--ink);border:1px solid #a8b8ce;border-radius:6px;cursor:pointer}
.doc-map-preview-content{padding:14px clamp(14px,4vw,28px) 24px;overflow:auto}
.doc-map-preview-content>h1:first-child{margin-top:.25em}
`;
