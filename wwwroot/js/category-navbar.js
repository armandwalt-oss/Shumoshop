// Category Navbar / Shop by Department Dropdown
(function () {
    'use strict';

    const toggleBtn = document.getElementById('menuToggle');
    const menu = document.getElementById('flyoutMenu');

    if (!toggleBtn || !menu) {
        console.warn('Category navbar elements not found');
        return;
    }

    const openMenu = () => {
        menu.classList.add('active');
        toggleBtn.setAttribute('aria-expanded', 'true');
        document.body.classList.add('menu-open');
    };

    const closeMenu = () => {
        menu.classList.remove('active');
        toggleBtn.setAttribute('aria-expanded', 'false');
        document.body.classList.remove('menu-open');
        // Close any expanded subcategories
        menu.querySelectorAll('.category-item-vertical.expanded').forEach(item => {
            item.classList.remove('expanded');
        });
    };

    const isOpen = () => menu.classList.contains('active');

    // Toggle menu on button click
    toggleBtn.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        isOpen() ? closeMenu() : openMenu();
    });

    // Close when clicking outside
    document.addEventListener('click', function (e) {
        if (!isOpen()) return;
        if (menu.contains(e.target) || toggleBtn.contains(e.target)) return;
        closeMenu();
    });

    // Close on ESC key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && isOpen()) {
            closeMenu();
        }
    });

    // Handle subcategory expansion in the dropdown
    const categoryItems = menu.querySelectorAll('.category-item-vertical.has-subs');
    categoryItems.forEach(item => {
        const arrowBtn = item.querySelector('.arrow-down');
        const subcategoryPanel = item.querySelector('.subcategory-panel');

        if (arrowBtn && subcategoryPanel) {
            arrowBtn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();

                const isExpanded = item.classList.contains('expanded');

                // Close all other expanded items
                categoryItems.forEach(otherItem => {
                    if (otherItem !== item) {
                        otherItem.classList.remove('expanded');
                        const otherArrow = otherItem.querySelector('.arrow-down');
                        if (otherArrow) {
                            otherArrow.setAttribute('aria-expanded', 'false');
                        }
                    }
                });

                // Toggle this item
                item.classList.toggle('expanded');
                arrowBtn.setAttribute('aria-expanded', !isExpanded);
            });
        }
    });

    // Close menu when a category link is clicked
    const categoryLinks = menu.querySelectorAll('.category-link, .subcategory-link');
    categoryLinks.forEach(link => {
        link.addEventListener('click', () => {
            closeMenu();
        });
    });

})();
