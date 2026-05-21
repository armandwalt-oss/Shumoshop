// Wishlist functionality for PARP E-commerce

// Add to wishlist function
function addToWishlist(productId) {
    $.ajax({
        url: '/Wishlist/AddToWishlist',
        type: 'POST',
        data: { productId: productId },
        success: function (response) {
            if (response.success) {
                // Update wishlist count in header
                updateWishlistCount(response.wishlistCount);

                // Show success notification
                showWishlistNotification('success', 'Added to wishlist!');

                // Change heart icon to filled
                updateHeartIcon(productId, true);
            } else {
                showWishlistNotification('info', response.message);
            }
        },
        error: function () {
            showWishlistNotification('error', 'Error adding to wishlist');
        }
    });
}

// Remove from wishlist function
function removeFromWishlist(productId) {
    $.ajax({
        url: '/Wishlist/RemoveFromWishlist',
        type: 'POST',
        data: { productId: productId },
        success: function (response) {
            if (response.success) {
                // Update wishlist count in header
                updateWishlistCount(response.wishlistCount);

                // Show success notification
                showWishlistNotification('success', 'Removed from wishlist');

                // Change heart icon back to outline
                updateHeartIcon(productId, false);
            } else {
                showWishlistNotification('error', response.message);
            }
        },
        error: function () {
            showWishlistNotification('error', 'Error removing from wishlist');
        }
    });
}

// Toggle wishlist (add or remove)
function toggleWishlist(productId) {
    const heartBtn = event.target.closest('.btn-wishlist');
    if (!heartBtn) return;

    const isInWishlist = heartBtn.classList.contains('in-wishlist');

    if (isInWishlist) {
        removeFromWishlist(productId);
    } else {
        addToWishlist(productId);
    }
}

// Update wishlist count badge
function updateWishlistCount(count) {
    $('.wishlist-count').text('(' + count + ')');

    // Add animation
    $('.wishlist-link').addClass('wishlist-bounce');
    setTimeout(function () {
        $('.wishlist-link').removeClass('wishlist-bounce');
    }, 300);
}

// Update heart icon (filled or outline)
function updateHeartIcon(productId, isFilled) {
    const heartBtn = $(`.btn-wishlist[data-product-id="${productId}"]`);
    const heartIcon = heartBtn.find('i');

    if (isFilled) {
        heartIcon.removeClass('fa-regular').addClass('fa-solid');
        heartBtn.addClass('in-wishlist');
    } else {
        heartIcon.removeClass('fa-solid').addClass('fa-regular');
        heartBtn.removeClass('in-wishlist');
    }
}

// Show wishlist notification
function showWishlistNotification(type, message) {
    // Remove existing notification
    $('.wishlist-notification').remove();

    // Icon based on type
    let icon = '';
    let bgColor = '';

    if (type === 'success') {
        icon = '<i class="fa-solid fa-heart"></i>';
        bgColor = 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)';
    } else if (type === 'info') {
        icon = '<i class="fa-solid fa-info-circle"></i>';
        bgColor = 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)';
    } else {
        icon = '<i class="fa-solid fa-exclamation-circle"></i>';
        bgColor = 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)';
    }

    // Create notification
    const notification = $('<div>')
        .addClass('wishlist-notification')
        .html(`
            <div class="wishlist-notification-content" style="background: ${bgColor}">
                <div class="wishlist-notification-icon">${icon}</div>
                <div class="wishlist-notification-message">${message}</div>
                <button class="wishlist-notification-close" onclick="closeWishlistNotification()">
                    <i class="fa-solid fa-times"></i>
                </button>
            </div>
        `)
        .appendTo('body');

    // Show notification with animation
    setTimeout(function () {
        notification.addClass('show');
    }, 10);

    // Auto hide after 3 seconds
    setTimeout(function () {
        closeWishlistNotification();
    }, 3000);
}

// Close wishlist notification
function closeWishlistNotification() {
    $('.wishlist-notification').removeClass('show');
    setTimeout(function () {
        $('.wishlist-notification').remove();
    }, 300);
}

// Load wishlist count on page load
$(document).ready(function () {
    $.ajax({
        url: '/Wishlist/GetWishlistCount',
        type: 'GET',
        success: function (response) {
            updateWishlistCount(response.count);
        }
    });
});

// Navigate to wishlist page
function goToWishlist() {
    window.location.href = '/Wishlist';
}