// Mobile table transposition for content tables.
//
// A Markdown table is row-major in the DOM: the header is the first row and every data record is a
// row beneath it. On a narrow screen that grid is too wide to read, so this module builds a
// *transposed* companion table where the original header row becomes a left-hand label column and
// each original data row becomes its own column. CSS (see app.css) shows the original on desktop
// and the transposed copy on mobile.
//
// Progressive enhancement: if this module never runs the original table still renders and scrolls
// horizontally, so nothing is lost without JS. We build the transposed copy once per table and mark
// the source with data-transposed-done so repeat renders of MarkupString content are cheap no-ops.

const PROCESSED = 'data-transposed-done';

// Read a table into a 2D array of { html, tag } cells, preserving inline markup and header-ness.
function readGrid(table) {
    const rows = [];
    for (const tr of table.rows) {
        const cells = [];
        for (const cell of tr.cells) {
            cells.push({ html: cell.innerHTML, isHeader: cell.tagName === 'TH' });
        }
        rows.push(cells);
    }
    return rows;
}

// Build a transposed <table> from the source grid: original columns become rows. The first original
// column (the row's identity) is rendered as a <th> scope="row" so it reads as the label column.
function buildTransposed(grid) {
    const transposed = document.createElement('table');
    transposed.className = 'table--transposed';
    transposed.setAttribute('aria-hidden', 'true');

    const columnCount = grid.reduce((max, row) => Math.max(max, row.length), 0);
    const body = document.createElement('tbody');

    for (let col = 0; col < columnCount; col++) {
        const tr = document.createElement('tr');

        for (let rowIndex = 0; rowIndex < grid.length; rowIndex++) {
            const source = grid[rowIndex][col];
            if (source === undefined) {
                continue;
            }

            // First original column stays a header (now the left label column); the original header
            // row's cells also stay headers. Everything else is a data cell.
            const asHeader = col === 0 || source.isHeader;
            const cell = document.createElement(asHeader ? 'th' : 'td');
            if (asHeader) {
                cell.setAttribute('scope', col === 0 ? 'row' : 'col');
            }
            cell.innerHTML = source.html;
            tr.appendChild(cell);
        }

        body.appendChild(tr);
    }

    transposed.appendChild(body);
    return transposed;
}

function enhanceTable(table) {
    if (table.hasAttribute(PROCESSED) || table.classList.contains('table--transposed')) {
        return;
    }

    const grid = readGrid(table);
    // A transpose only helps when there is a header row plus at least one data row and more than one
    // column; otherwise leave the original alone.
    if (grid.length < 2 || grid[0].length < 2) {
        table.setAttribute(PROCESSED, '');
        return;
    }

    const transposed = buildTransposed(grid);
    table.setAttribute(PROCESSED, '');
    table.insertAdjacentElement('afterend', transposed);
}

// Find every content table under the root and give it a transposed companion. Safe to call on every
// render: already-processed tables are skipped.
export function enhance(root) {
    if (!root) {
        return;
    }

    const tables = root.querySelectorAll('table:not(.table--transposed)');
    tables.forEach(enhanceTable);
}

// Remove generated companions under the root (called on teardown to avoid leaking orphaned nodes).
export function dispose(root) {
    if (!root) {
        return;
    }

    root.querySelectorAll('.table--transposed').forEach((el) => el.remove());
    root.querySelectorAll('[' + PROCESSED + ']').forEach((el) => el.removeAttribute(PROCESSED));
}
