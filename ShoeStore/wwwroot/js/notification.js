// Biến để kiểm tra connection đã được tạo chưa
let isConnected = false;
let processedNotifications = new Set(); // Thêm Set để theo dõi thông báo đã xử lý

// Cấu hình toastr
toastr.options = {
    "closeButton": true,
    "progressBar": true,
    "positionClass": "toast-top-right",
    "timeOut": "5000",
    "debug": true  // Enable debug mode
};

let notificationCount = 0;

// Khởi tạo SignalR connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Debug)  // Enable debug logging
    .build();

// Hàm format thời gian
function formatTime(date) {
    const now = new Date();
    const diff = now - new Date(date);
    
    if (diff < 60000) return 'Vừa xong';
    if (diff < 3600000) return Math.floor(diff/60000) + ' phút trước';
    if (diff < 86400000) return Math.floor(diff/3600000) + ' giờ trước';
    return Math.floor(diff/86400000) + ' ngày trước';
}

// Thêm hàm lấy icon cho từng loại thông báo
function getNotificationIcon(type) {
    switch(type) {
        case 'new': return 'fas fa-shopping-cart';
        case 'cancel': return 'fas fa-times-circle';
        case 'return': return 'fas fa-undo';
        default: return 'fas fa-bell';
    }
}

// Thêm hàm mới để lấy class cho từng loại thông báo
function getNotificationClass(type) {
    switch(type) {
        case 'new': return 'notif-success';
        case 'cancel': return 'notif-danger';
        case 'return': return 'notif-warning';
        default: return 'notif-primary';
    }
}

// Cập nhật hàm loadUnreadNotifications
function loadUnreadNotifications() {
    $.get('/Admin/Notification/GetUnreadNotifications', function(notifications) {
        console.log("Loaded notifications:", notifications);
        $('#notificationList').empty();
        notificationCount = notifications.length;
        $('.notif-count').text(notificationCount);
        
        notifications.forEach(notification => {
            const icon = getNotificationIcon(notification.type);
            const html = `
                <a href="${notification.url}" onclick="markAsRead(${notification.id}, event)">
                    <div class="notif-icon ${getNotificationClass(notification.type)}">
                        <i class="${icon}"></i>
                    </div>
                    <div class="notif-content">
                        <span class="block">${notification.message}</span>
                        <span class="time">${formatTime(notification.createdAt)}</span>
                    </div>
                </a>
            `;
            $('#notificationList').append(html);
        });
    });
}

// Đánh dấu thông báo đã đọc
function markAsRead(id, event) {
    event.preventDefault();
    const link = $(event.currentTarget).attr('href');
    $.post('/Admin/Notification/MarkAsRead/' + id, function() {
        updateNotificationCount(-1);
        $(event.currentTarget).fadeOut();
        // Sau khi đánh dấu đã đọc, chuyển hướng đến trang chi tiết
        window.location.href = link;
    });
}

// Đánh dấu tất cả là đã đọc
function markAllAsRead() {
    $.post('/Admin/Notification/MarkAllAsRead', function() {
        $('#notificationList').empty();
        updateNotificationCount(-notificationCount);
    });
}

// Cập nhật số lượng thông báo
function updateNotificationCount(increment = 0) {
    notificationCount += increment;
    $('.notif-count').text(notificationCount);
}

// Xử lý thông báo đơn hàng
connection.on("ReceiveOrderNotification", (message, orderId, type) => {
    // Tạo một key duy nhất cho thông báo
    const notificationKey = `${message}-${orderId}-${type}`;
    
    // Kiểm tra xem thông báo đã được xử lý chưa
    if (processedNotifications.has(notificationKey)) {
        console.log('Duplicate notification, skipping:', notificationKey);
        return;
    }
    
    // Thêm thông báo vào danh sách đã xử lý
    processedNotifications.add(notificationKey);
    
    console.log("Received order notification:", { message, orderId, type });
    
    // Phát âm thông báo
    const audio = new Audio('/audio/notification.mp3');
    audio.play();

    // Hiển thị toast thông báo
    switch(type) {
        case 'new':
            toastr.success(message, 'Đơn hàng mới');
            break;
        case 'cancel':
            toastr.warning(message, 'Đơn hàng bị hủy');
            break;
        case 'return':
            toastr.info(message, 'Yêu cầu đổi/trả');
            break;
        default:
            toastr.info(message);
    }

    // Tải lại danh sách thông báo
    loadUnreadNotifications();
    
    // Xóa thông báo khỏi danh sách đã xử lý sau 5 giây
    setTimeout(() => {
        processedNotifications.delete(notificationKey);
    }, 5000);
});

// Xử lý thông báo chung
connection.on("ReceiveNotification", (message, type) => {
    console.log("Received general notification:", { message, type });
    
    switch(type) {
        case 'success':
            toastr.success(message);
            break;
        case 'error':
            toastr.error(message);
            break;
        case 'warning':
            toastr.warning(message);
            break;
        default:
            toastr.info(message);
    }

    // Tải lại danh sách thông báo
    loadUnreadNotifications();
});

// Kết nối đến SignalR hub
function startConnection() {
    if (isConnected) {
        console.log('SignalR already connected!');
        return;
    }

    connection.start()
        .then(() => {
            console.log('SignalR Connected!');
            isConnected = true;
            loadUnreadNotifications();
        })
        .catch(err => {
            console.error('Lỗi kết nối SignalR:', err);
            isConnected = false;
            // Thử kết nối lại sau 5 giây
            setTimeout(startConnection, 5000);
        });
}

connection.onclose(() => {
    console.log('SignalR Disconnected! Attempting to reconnect...');
    isConnected = false;
    startConnection();
});

// Document ready handlers
$(document).ready(() => {
    // Bắt đầu kết nối SignalR
    startConnection();

    // Ngăn dropdown tự động đóng khi click vào nội dung
    $('.dropdown-notif').on('click', function(e) {
        e.stopPropagation();
    });

    // Cập nhật UI khi mở dropdown
    $('#notificationDropdown').on('show.bs.dropdown', function () {
        loadUnreadNotifications();
    });

    // Xử lý click vào nút "Xem tất cả thông báo"
    $('.dropdown-footer').click(function(e) {
        e.preventDefault();
        markAllAsRead();
    });
}); 