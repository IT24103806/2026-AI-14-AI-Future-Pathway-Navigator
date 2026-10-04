import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/utils/date_format.dart';
import '../../../core/widgets/common_widgets.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/consultation_models.dart';
import 'notification_controller.dart';

/// The in-app notification inbox.
///
/// A notification is the signal that a consultant answered or that a case moved; tapping one opens
/// the exact component the answer was written back into, so the student can continue their journey
/// from there.
class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    // Uses the app-wide controller so the badge in the app bars tracks the same unread count.
    final controller = context.read<NotificationController>();
    return _NotificationsLoader(controller: controller);
  }
}

/// Kicks off the first page load exactly once, then renders [NotificationsScreen]' body.
class _NotificationsLoader extends StatefulWidget {
  const _NotificationsLoader({required this.controller});

  final NotificationController controller;

  @override
  State<_NotificationsLoader> createState() => _NotificationsLoaderState();
}

class _NotificationsLoaderState extends State<_NotificationsLoader> {
  @override
  void initState() {
    super.initState();
    widget.controller.load();
  }

  @override
  Widget build(BuildContext context) => const _NotificationsBody();
}

class _NotificationsBody extends StatelessWidget {
  const _NotificationsBody();

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<NotificationController>();
    final role = context.watch<AuthController>().role ?? '';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Notifications'),
        actions: [
          TextButton(
            onPressed: controller.unreadCount == 0 ? null : controller.markAllRead,
            child: const Text('Mark all read'),
          ),
        ],
      ),
      body: controller.isLoading && controller.page.items.isEmpty
          ? const LoadingView()
          : controller.page.items.isEmpty
              ? const MessageView(
                  icon: Icons.notifications_none,
                  title: 'Nothing new',
                  message: 'Updates about your questions and reviews will appear here.',
                )
              : Column(
                  children: [
                    if (controller.error != null)
                      Padding(padding: const EdgeInsets.all(16), child: ErrorBanner(message: controller.error!)),
                    Expanded(
                      child: RefreshIndicator(
                        onRefresh: () => controller.load(),
                        child: ListView.separated(
                          padding: const EdgeInsets.all(16),
                          itemCount: controller.page.items.length,
                          separatorBuilder: (_, _) => const SizedBox(height: 8),
                          itemBuilder: (context, index) {
                            final item = controller.page.items[index];
                            return Card(
                              child: ListTile(
                                leading: Icon(
                                  item.isUrgent ? Icons.priority_high : Icons.notifications_outlined,
                                  color: item.isRead ? null : Theme.of(context).colorScheme.primary,
                                ),
                                title: Text(item.title, style: TextStyle(fontWeight: item.isRead ? FontWeight.normal : FontWeight.bold)),
                                subtitle: Text('${item.body}\n${formatDateTime(item.createdAt)}'),
                                isThreeLine: true,
                                onTap: () async {
                                  await controller.markRead(item.id);
                                  if (!context.mounted) return;
                                  final route = notificationRoute(item.deepLink, role);
                                  if (route.isNotEmpty) context.push(route);
                                },
                              ),
                            );
                          },
                        ),
                      ),
                    ),
                  ],
                ),
    );
  }
}

/// The unread badge shown in app bars.
class NotificationBell extends StatelessWidget {
  const NotificationBell({super.key, required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<NotificationController>();
    return IconButton(
      tooltip: 'Notifications',
      onPressed: onPressed,
      icon: Badge(
        isLabelVisible: controller.unreadCount > 0,
        label: Text('${controller.unreadCount}'),
        child: const Icon(Icons.notifications_outlined),
      ),
    );
  }
}
