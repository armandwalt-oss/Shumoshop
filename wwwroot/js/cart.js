// Cart functionality for PARP E-commerce

// Add to cart function
function addToCart(productId, quantity = 1) {
    // Add loading state to button if it exists
    const clickedButton = event?.target?.closest('button');
    if (clickedButton) {
        clickedButton.disabled = true;
        clickedButton.style.opacity = '0.7';
    }

    $.ajax({
        url: '/Cart/AddToCart',
        type: 'POST',
        data: { productId: productId, quantity: quantity },
        success: function (response) {
            if (response.success) {
                // Update cart count in header
                updateCartCount(response.cartCount);

                // Show beautiful popup notification
                showCartPopup('success', 'Product added to cart!', quantity);
            } else {
                showCartPopup('error', response.message);
            }
        },
        error: function () {
            showCartPopup('error', 'Error adding product to cart');
        },
        complete: function () {
            // Remove loading state
            if (clickedButton) {
                clickedButton.disabled = false;
                clickedButton.style.opacity = '1';
            }
        }
    });
}

// Update cart count badge
function updateCartCount(count) {
    $('.cart-count').text('(' + count + ')');

    // Add animation
    $('.cart-link').addClass('cart-bounce');
    setTimeout(function () {
        $('.cart-link').removeClass('cart-bounce');
    }, 300);
}

// Beautiful popup notification
function showCartPopup(type, message, quantity = 1) {
    // Remove existing popup
    $('.cart-popup').remove();

    // Icon based on type
    let icon = '';
    let bgColor = '';

    if (type === 'success') {
        icon = '<i class="fa-solid fa-circle-check"></i>';
        bgColor = 'linear-gradient(135deg, #10b981 0%, #059669 100%)';
    } else {
        icon = '<i class="fa-solid fa-circle-exclamation"></i>';
        bgColor = 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)';
    }

    // Create popup
    const popup = $('<div>')
        .addClass('cart-popup')
        .html(`
            <div class="cart-popup-content" style="background: ${bgColor}">
                <div class="cart-popup-icon">${icon}</div>
                <div class="cart-popup-message">
                    <div class="cart-popup-title">${type === 'success' ? 'Success!' : 'Oops!'}</div>
                    <div class="cart-popup-text">${message}</div>
                    ${type === 'success' ? '<div class="cart-popup-quantity">Quantity: ' + quantity + '</div>' : ''}
                </div>
                <button class="cart-popup-close" onclick="closeCartPopup()">
                    <i class="fa-solid fa-times"></i>
                </button>
            </div>
            ${type === 'success' ? `
            <div class="cart-popup-actions">
                <button class="btn-view-cart" onclick="window.location.href='/Cart'">
                    <i class="fa-solid fa-shopping-cart"></i> View Cart
                </button>
                <button class="btn-continue-shopping" onclick="closeCartPopup()">
                    Continue Shopping
                </button>
            </div>
            ` : ''}
        `)
        .appendTo('body');

    // Show popup with animation
    setTimeout(function () {
        popup.addClass('show');
    }, 10);

    // Auto hide after 5 seconds
    setTimeout(function () {
        closeCartPopup();
    }, 5000);
}

// Close popup
function closeCartPopup() {
    $('.cart-popup').removeClass('show');
    setTimeout(function () {
        $('.cart-popup').remove();
    }, 300);
}

// Load cart count on page load
$(document).ready(function () {
    $.ajax({
        url: '/Cart/GetCartCount',
        type: 'GET',
        success: function (response) {
            updateCartCount(response.count);
        }
    });
});

// Navigate to cart when cart button clicked
$(document).on('click', '.cart-link', function (e) {
    if (!$(e.target).closest('[data-href]').length) {
        window.location.href = '/Cart';
    }
});