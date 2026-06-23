// Premium High-Tech Light/Dark Theme Switcher Logic
document.addEventListener("DOMContentLoaded", function () {
    const body = document.body;
    const themeBtn = document.getElementById("themeToggleBtn");
    
    if (themeBtn) {
        const darkIcon = themeBtn.querySelector(".theme-icon-dark");
        const lightIcon = themeBtn.querySelector(".theme-icon-light");

    // 1. Load theme preference from localStorage or default to current HTML class
    const savedTheme = localStorage.getItem("theme");
    if (savedTheme) {
        body.classList.remove("theme-light", "theme-dark");
        body.classList.add(savedTheme);
    }

    // 2. Adjust toggle button icon states initially
    updateThemeIcons();

    // 3. Add click event listener to the switcher button
    themeBtn.addEventListener("click", function () {
        if (body.classList.contains("theme-light")) {
            body.classList.replace("theme-light", "theme-dark");
            localStorage.setItem("theme", "theme-dark");
        } else {
            body.classList.replace("theme-dark", "theme-light");
            localStorage.setItem("theme", "theme-light");
        }
        updateThemeIcons();
    });

    function updateThemeIcons() {
        if (body.classList.contains("theme-dark")) {
            darkIcon.style.display = "none";
            lightIcon.style.display = "inline-block";
        } else {
            darkIcon.style.display = "inline-block";
            lightIcon.style.display = "none";
        }
    }
    } // Close if (themeBtn)

    // 4. Scroll Reveal Animation for Product Cards
    const cards = document.querySelectorAll(".product-card");
    if (cards.length > 0) {
        cards.forEach((card, index) => {
            card.style.opacity = "0";
            card.style.transform = "translateY(25px)";
            card.style.transition = "opacity 0.6s cubic-bezier(0.4, 0, 0.2, 1), transform 0.6s cubic-bezier(0.4, 0, 0.2, 1)";
            card.style.transitionDelay = `${(index % 3) * 0.1}s`;
        });

        const revealObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = "1";
                    entry.target.style.transform = "translateY(0)";
                    observer.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.1,
            rootMargin: "0px 0px -50px 0px"
        });

        cards.forEach(card => revealObserver.observe(card));
    }

    // --- SHOPPING CART AJAX SYSTEM ---
    
    // Toast Notification System
    const toastContainer = document.getElementById("toastContainer");
    
    function showToast(message, type = "success") {
        if (!toastContainer) return;
        
        const toast = document.createElement("div");
        toast.className = "toast-custom";
        
        let iconHtml = '<i class="fa-solid fa-circle-check toast-icon-custom"></i>';
        if (type === "error") {
            iconHtml = '<i class="fa-solid fa-circle-exclamation toast-icon-custom" style="color: #ef4444;"></i>';
            toast.style.borderLeftColor = "#ef4444";
        }
        
        toast.innerHTML = `
            ${iconHtml}
            <div class="toast-content-custom">${message}</div>
            <button type="button" class="toast-close-custom" onclick="this.parentElement.remove()">
                <i class="fa-solid fa-xmark"></i>
            </button>
        `;
        
        toastContainer.appendChild(toast);
        
        // Trigger reflow to start transition
        toast.offsetHeight;
        toast.classList.add("show");
        
        // Auto-remove after 4 seconds
        setTimeout(() => {
            toast.classList.remove("show");
            setTimeout(() => {
                toast.remove();
            }, 400);
        }, 4000);
    }

    function formatVND(value) {
        return new Intl.NumberFormat('vi-VN').format(value) + ' đ';
    }

    function updateCartBadge(count) {
        const badge = document.getElementById("cartCountBadge");
        if (!badge) return;
        
        badge.innerText = count;
        if (count > 0) {
            badge.style.display = "flex";
            badge.classList.remove("bounce-animation");
            // Force redraw/reflow
            badge.offsetWidth;
            badge.classList.add("bounce-animation");
        } else {
            badge.style.display = "none";
        }
    }

    // Intercept Add to Cart clicks
    document.addEventListener("click", function (event) {
        const btn = event.target.closest(".btn-add-to-cart");
        if (!btn) return;
        
        event.preventDefault();
        event.stopPropagation();
        
        const productId = btn.getAttribute("data-product-id");
        if (!productId) return;
        
        // Add loading state style
        const originalHtml = btn.innerHTML;
        btn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i>';
        btn.disabled = true;
        
        $.ajax({
            url: "/Cart/AddToCart",
            type: "POST",
            data: { productId: productId, quantity: 1 },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                if (response.requiresLogin) {
                    showToast('<a href="/Account/Login" style="color: inherit; text-decoration: underline;">Đăng nhập</a> để thêm sản phẩm vào giỏ hàng!', "error");
                } else if (response.success) {
                    updateCartBadge(response.cartCount);
                    showToast("Đã thêm sản phẩm vào giỏ hàng!");
                } else {
                    showToast("Có lỗi xảy ra khi thêm vào giỏ hàng.", "error");
                }
            },
            error: function () {
                showToast("Không thể kết nối đến hệ thống.", "error");
            },
            complete: function () {
                btn.innerHTML = originalHtml;
                btn.disabled = false;
            }
        });
    });

    // Cart Page quantity buttons & deletion
    document.addEventListener("click", function (event) {
        // 1. Decrease Quantity
        const decreaseBtn = event.target.closest(".btn-decrease");
        if (decreaseBtn) {
            const productId = decreaseBtn.getAttribute("data-product-id");
            const input = document.querySelector(`.quantity-input[data-product-id="${productId}"]`);
            if (!input) return;
            
            let currentQty = parseInt(input.value) || 1;
            const newQty = currentQty - 1;
            
            updateQuantityAjax(productId, newQty, input);
        }
        
        // 2. Increase Quantity
        const increaseBtn = event.target.closest(".btn-increase");
        if (increaseBtn) {
            const productId = increaseBtn.getAttribute("data-product-id");
            const input = document.querySelector(`.quantity-input[data-product-id="${productId}"]`);
            if (!input) return;
            
            let currentQty = parseInt(input.value) || 1;
            const newQty = currentQty + 1;
            
            updateQuantityAjax(productId, newQty, input);
        }
        
        // 3. Remove Item (intercept form submit - no confirm dialog needed)
        const removeBtn = event.target.closest(".btn-remove-item");
        if (removeBtn) {
            event.preventDefault();
            event.stopPropagation();
            
            const form = removeBtn.closest(".remove-item-form");
            const productId = removeBtn.getAttribute("data-product-id") || form?.querySelector('input[name="productId"]')?.value;
            if (!productId) {
                if (form) HTMLFormElement.prototype.submit.call(form);
                return;
            }
            removeItemAjax(productId, form);
        }
        
        // 4. Clear Cart
        const clearBtn = event.target.closest(".btn-clear-cart");
        if (clearBtn) {
            clearCartAjax();
        }
    });

    function updateQuantityAjax(productId, quantity, inputElement) {
        $.ajax({
            url: "/Cart/UpdateQuantity",
            type: "POST",
            data: { productId: productId, quantity: quantity },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                if (response.success) {
                    updateCartBadge(response.cartCount);
                    
                    if (response.removed) {
                        // Remove row from DOM with animation
                        const row = document.querySelector(`.cart-item-row[data-product-id="${productId}"]`);
                        if (row) {
                            row.style.opacity = "0";
                            row.style.transform = "translateX(-20px)";
                            setTimeout(() => {
                                row.remove();
                                checkCartEmptyState(response.cartCount);
                            }, 300);
                        }
                        updateCartTotals(response.cartTotal, response.shippingFee, response.grandTotal);
                        showToast("Đã xóa sản phẩm khỏi giỏ hàng!");
                    } else {
                        // Update input value
                        inputElement.value = quantity;
                        // Update item subtotal
                        const subtotalSpan = document.querySelector(`.item-total-price[data-product-id="${productId}"]`);
                        if (subtotalSpan) {
                            subtotalSpan.innerText = formatVND(response.itemTotal);
                        }
                        // Update cart totals
                        updateCartTotals(response.cartTotal, response.shippingFee, response.grandTotal);
                    }
                } else {
                    showToast("Không thể cập nhật số lượng.", "error");
                }
            },
            error: function () {
                showToast("Không thể kết nối đến hệ thống.", "error");
            }
        });
    }

    function removeItemAjax(productId, fallbackForm) {
        $.ajax({
            url: "/Cart/RemoveItem",
            type: "POST",
            data: { productId: productId },
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                if (response.success) {
                    updateCartBadge(response.cartCount);
                    
                    // Remove row from DOM
                    const row = document.querySelector(`.cart-item-row[data-product-id="${productId}"]`);
                    if (row) {
                        row.style.opacity = "0";
                        row.style.transform = "translateX(-20px)";
                        setTimeout(() => {
                            row.remove();
                            checkCartEmptyState(response.cartCount);
                        }, 300);
                    }
                    
                    updateCartTotals(response.cartTotal, response.shippingFee, response.grandTotal);
                    showToast("Đã xóa sản phẩm khỏi giỏ hàng!");
                } else {
                    showToast("Không thể xóa sản phẩm.", "error");
                }
            },
            error: function () {
                if (fallbackForm) {
                    HTMLFormElement.prototype.submit.call(fallbackForm);
                    return;
                }
                showToast("Không thể kết nối đến hệ thống.", "error");
            }
        });
    }

    function clearCartAjax() {
        $.ajax({
            url: "/Cart/ClearCart",
            type: "POST",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                if (response.success) {
                    updateCartBadge(0);
                    
                    const list = document.getElementById("cartItemsList");
                    if (list) {
                        list.style.opacity = "0";
                        setTimeout(() => {
                            list.innerHTML = "";
                            checkCartEmptyState(0);
                        }, 300);
                    }
                    updateCartTotals(0, 0, 0);
                    showToast("Đã xóa toàn bộ giỏ hàng!");
                } else {
                    showToast("Không thể xóa giỏ hàng.", "error");
                }
            },
            error: function () {
                showToast("Không thể kết nối đến hệ thống.", "error");
            }
        });
    }

    function updateCartTotals(total, shippingFee, finalTotal) {
        const subtotal = document.getElementById("cartSubtotal");
        const shipping = document.getElementById("cartShippingFee");
        const grandTotalElement = document.getElementById("cartTotal");
        
        if (subtotal) subtotal.innerText = formatVND(total);
        if (shipping) shipping.innerText = shippingFee > 0 ? formatVND(shippingFee) : "Miễn phí";
        if (grandTotalElement) grandTotalElement.innerText = formatVND(finalTotal ?? total);
    }

    function checkCartEmptyState(cartCount) {
        if (cartCount === 0) {
            const emptyState = document.getElementById("emptyCartState");
            const contentState = document.getElementById("cartContentState");
            
            if (emptyState && contentState) {
                contentState.style.display = "none";
                emptyState.style.display = "block";
                
                // Trigger animation for empty showroom
                emptyState.style.opacity = "0";
                emptyState.style.transform = "translateY(15px)";
                emptyState.offsetHeight; // force reflow
                emptyState.style.transition = "opacity 0.4s ease, transform 0.4s ease";
                emptyState.style.opacity = "1";
                emptyState.style.transform = "translateY(0)";
            }
        }
    }
});
