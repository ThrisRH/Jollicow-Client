function getCartKey() {
  const idTable = document.body.dataset.table; // Hoặc lấy từ ViewData["IdTable"]
  const restaurantId = document.body.dataset.restaurant; // Hoặc ViewData["RestaurantId"]
  return `cartItems_${idTable}_${restaurantId}`;
}

// Hàm fetch
async function apiFetch(
  url,
  method = "GET",
  data = null,
  headers = { "Content-Type": "application/json" }
) {
  const options = {
    method,
    headers,
  };
  if (data) {
    options.body = JSON.stringify(data);
  }
  const response = await fetch(url, options);
  console.log("fetch: ", response);
  if (!response.ok) {
    throw new Error(`API error: ${response.status}`);
  }
  return response.json();
}

async function updateCartTotalUI() {
  try {
    // Ưu tiên đọc từ localStorage trước
    const savedTotal = localStorage.getItem("cart_total");
    if (savedTotal) {
      const total = parseFloat(savedTotal);
      console.log("Using saved cart total from localStorage:", total);

      const cartTotalElement = document.getElementById("cartTotalPrice");
      if (cartTotalElement) {
        cartTotalElement.textContent = total.toLocaleString("vi-VN") + "đ";
      }
      return; // Không cần gọi API nếu đã có dữ liệu
    }

    // Nếu không có localStorage, gọi API
    const payload = {
      id_table: document.body.dataset.table,
      id_restaurant: document.body.dataset.restaurant,
    };

    // console.log("payload:", payload);

    const response = await apiFetch(
      "https://jollicowfe-production.up.railway.app/api/admin/carts/filter",
      "POST",
      payload
    );
    const cartItems = response.carts || [];
    console.log("Cart", cartItems);
    // const cartItems = JSON.parse(localStorage.getItem(cartKey) || "[]");

    const total = cartItems.reduce(
      (sum, item) =>
        sum + parseFloat(item.price) * (parseInt(item.quantity) || 1),
      0
    );

    localStorage.setItem("cart_total", total);

    const cartTotalElement = document.getElementById("cartTotalPrice");
    if (cartTotalElement) {
      cartTotalElement.textContent = total.toLocaleString("vi-VN") + "đ";
    }
  } catch (error) {
    console.error("Error updating cart total:", error);
    const cartKey = getCartKey();
    localStorage.setItem(cartKey, "[]");

    const cartTotalElement = document.getElementById("cartTotalPrice");
    if (cartTotalElement) {
      cartTotalElement.textContent = "0đ";
    }
  }
}

// Hàm cập nhật tổng tiền từ API với kiểm tra thay đổi
async function updateCartTotalFromAPI() {
  try {
    const payload = {
      id_table: document.body.dataset.table,
      id_restaurant: document.body.dataset.restaurant,
    };

    const response = await apiFetch(
      "https://jollicowfe-production.up.railway.app/api/admin/carts/filter",
      "POST",
      payload
    );
    const cartItems = response.carts || [];
    console.log("Cart from API:", cartItems);

    const total = cartItems.reduce(
      (sum, item) =>
        sum + parseFloat(item.price) * (parseInt(item.quantity) || 1),
      0
    );

    // Chỉ cập nhật nếu tổng tiền thực sự thay đổi
    if (total !== lastCartTotal) {
      console.log(`Cart total changed from ${lastCartTotal} to ${total}`);

      // Cập nhật localStorage
      localStorage.setItem("cart_total", total.toString());

      // Cập nhật UI
      const cartTotalElement = document.getElementById("cartTotalPrice");
      if (cartTotalElement) {
        cartTotalElement.textContent = total.toLocaleString("vi-VN") + "đ";
      }

      lastCartTotal = total;
    } else {
      console.log("Cart total unchanged:", total);
    }

    return total;
  } catch (error) {
    console.error("Error updating cart total from API:", error);
    return lastCartTotal; // Giữ nguyên giá trị cũ nếu lỗi
  }
}

// Polling thông minh - chỉ khi cần thiết
let cartPollingInterval;
let lastUpdateTime = 0;
const UPDATE_INTERVAL = 30000; // 30 giây
let lastCartTotal = 0; // Lưu tổng tiền cuối cùng để so sánh

function startSmartCartPolling() {
  // Cập nhật ngay lập tức
  updateCartTotalFromAPI();

  // Polling mỗi 30 giây thay vì 3 giây
  cartPollingInterval = setInterval(() => {
    const now = Date.now();
    if (now - lastUpdateTime > UPDATE_INTERVAL) {
      updateCartTotalFromAPI();
      lastUpdateTime = now;
    }
  }, 30000);
}

function stopCartPolling() {
  if (cartPollingInterval) {
    clearInterval(cartPollingInterval);
    cartPollingInterval = null;
  }
}

// Gọi khi trang load
document.addEventListener("DOMContentLoaded", function () {
  // Cập nhật lần đầu
  updateCartTotalUI();

  // Không cần polling nữa - chỉ dựa vào events
  // startSmartCartPolling();
});

// Gọi khi localStorage thay đổi (nếu dùng nhiều tab)
window.addEventListener("storage", function (e) {
  if (e.key === getCartKey() || e.key === "cart_total") {
    updateCartTotalUI();
  }
});

// Lắng nghe event cartTotalUpdated từ cart page
window.addEventListener("cartTotalUpdated", function (e) {
  const total = e.detail.total;
  console.log("Cart total updated event received:", total);

  // Cập nhật navbar
  const cartTotalElement = document.getElementById("cartTotalPrice");
  if (cartTotalElement) {
    cartTotalElement.textContent = total.toLocaleString("vi-VN") + "đ";
  }

  // Lưu vào localStorage
  localStorage.setItem("cart_total", total.toString());
});

// Cập nhật khi user tương tác với trang
window.addEventListener("focus", function () {
  console.log("Tab focused - updating cart total");
  updateCartTotalUI();
});

document.addEventListener("visibilitychange", function () {
  if (!document.hidden) {
    console.log("Tab visible - updating cart total");
    updateCartTotalUI();
  }
});

// Cleanup khi rời trang
window.addEventListener("beforeunload", function () {
  stopCartPolling();
});
