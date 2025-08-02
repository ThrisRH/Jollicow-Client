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

// Gọi khi trang load
document.addEventListener("DOMContentLoaded", updateCartTotalUI);

// Gọi khi localStorage thay đổi (nếu dùng nhiều tab)
window.addEventListener("storage", function (e) {
  if (e.key === getCartKey()) {
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
