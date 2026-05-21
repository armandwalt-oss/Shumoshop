// wwwroot/js/table-filter.js
(function () {
    "use strict";

    function initTableFilter(root) {
        const input = root.querySelector("[data-search-input]");
        const countEl = root.querySelector("[data-search-count]");

        const tableContainerSelector = root.dataset.tableContainer;
        const rowSelector = root.dataset.rowSelector;
        const searchFieldsSelector = root.dataset.searchFields;
        const emptySelector = root.dataset.empty;

        const tableContainer = tableContainerSelector ? document.querySelector(tableContainerSelector) : null;
        const rows = rowSelector ? document.querySelectorAll(rowSelector) : [];

        const emptyEl = emptySelector ? document.querySelector(emptySelector) : null;

        if (!input || !countEl || !tableContainer || rows.length === 0 || !searchFieldsSelector) return;

        const getRowText = (row) => {
            const fields = row.querySelectorAll(searchFieldsSelector);
            return Array.from(fields)
                .map(x => (x.textContent || "").toLowerCase())
                .join(" | ");
        };

        const allRowText = Array.from(rows).map(r => ({ row: r, text: getRowText(r) }));

        const applyFilter = () => {
            const term = (input.value || "").toLowerCase().trim();
            let visibleCount = 0;

            allRowText.forEach(item => {
                const match = term === "" || item.text.includes(term);
                item.row.style.display = match ? "" : "none";
                if (match) visibleCount++;
            });

            countEl.textContent = visibleCount;

            if (term !== "" && visibleCount === 0) {
                tableContainer.style.display = "none";
                if (emptyEl) emptyEl.style.display = "block";
            } else {
                tableContainer.style.display = "block";
                if (emptyEl) emptyEl.style.display = "none";
            }
        };

        input.addEventListener("input", applyFilter);

        input.addEventListener("keydown", (e) => {
            if (e.key === "Escape") {
                input.value = "";
                applyFilter();
            }
        });

        applyFilter();
    }

    document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll("[data-table-filter]").forEach(initTableFilter);
    });
})();
