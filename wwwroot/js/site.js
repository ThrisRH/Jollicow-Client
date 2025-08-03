// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Hàm cập nhật tổng tiền giỏ hàng trên navbar
function updateNavbarCartTotal(total) {
  const cartTotalElement = document.getElementById("cartTotalPrice");
  if (cartTotalElement) {
    cartTotalElement.textContent = total.toLocaleString("vi-VN") + "đ";
  }
}

// Hàm lấy tổng tiền từ localStorage
function getCartTotalFromStorage() {
  const savedTotal = localStorage.getItem("cart_total");
  return savedTotal ? parseFloat(savedTotal) : 0;
}

// Hàm cập nhật navbar từ localStorage
function updateNavbarFromStorage() {
  const total = getCartTotalFromStorage();
  updateNavbarCartTotal(total);
}

// Hàm emit event để thông báo cập nhật tổng tiền
function emitCartTotalUpdate(total) {
  const event = new CustomEvent("cartTotalUpdated", {
    detail: { total: total },
  });
  window.dispatchEvent(event);
}

// Debounce function để tránh spam API
let updateTimeout;
function debouncedUpdate() {
  clearTimeout(updateTimeout);
  updateTimeout = setTimeout(() => {
    updateNavbarFromStorage();
  }, 1000); // Đợi 1 giây sau khi có thay đổi
}

// Custom Popup Functions
let popupCallback = null;

function showPopup(title, message, confirmText = "Xác nhận", callback = null) {
  const popup = document.getElementById("customPopup");
  const popupTitle = document.getElementById("popupTitle");
  const popupMessage = document.getElementById("popupMessage");
  const confirmBtn = document.getElementById("popupConfirmBtn");

  popupTitle.textContent = title;
  popupMessage.textContent = message;
  confirmBtn.textContent = confirmText;

  popupCallback = callback;

  popup.classList.add("show");

  // Disable body scroll
  document.body.style.overflow = "hidden";
}

function closePopup() {
  const popup = document.getElementById("customPopup");
  popup.classList.remove("show");

  // Re-enable body scroll
  document.body.style.overflow = "";

  popupCallback = null;
}

function confirmPopup() {
  if (popupCallback) {
    popupCallback();
  }
  closePopup();
}

// Custom confirm function thay thế cho confirm()
function customConfirm(message, callback) {
  showPopup("Xác nhận", message, "Xác nhận", callback);
}

// Khởi tạo khi trang load
document.addEventListener("DOMContentLoaded", function () {
  // Cập nhật navbar từ localStorage
  updateNavbarFromStorage();

  // Lắng nghe sự thay đổi localStorage
  window.addEventListener("storage", function (e) {
    if (e.key === "cart_total") {
      debouncedUpdate();
    }
  });

  // Cập nhật khi user tương tác với trang (focus, visibility change)
  window.addEventListener("focus", function () {
    // Cập nhật khi tab được focus
    updateNavbarFromStorage();
  });

  document.addEventListener("visibilitychange", function () {
    if (!document.hidden) {
      // Cập nhật khi tab trở nên visible
      updateNavbarFromStorage();
    }
  });

  // Cập nhật khi user click vào navbar (có thể đang xem giỏ hàng)
  const navbar = document.querySelector("nav");
  if (navbar) {
    navbar.addEventListener("click", function () {
      // Cập nhật khi user tương tác với navbar
      updateNavbarFromStorage();
    });
  }

  // Setup popup confirm button
  const confirmBtn = document.getElementById("popupConfirmBtn");
  if (confirmBtn) {
    confirmBtn.addEventListener("click", confirmPopup);
  }

  // Close popup when clicking overlay
  const popup = document.getElementById("customPopup");
  if (popup) {
    popup.addEventListener("click", function (e) {
      if (e.target === popup) {
        closePopup();
      }
    });
  }

  // Close popup with Escape key
  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape") {
      closePopup();
    }
  });
});
