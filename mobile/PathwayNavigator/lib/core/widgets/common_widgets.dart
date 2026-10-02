import 'package:flutter/material.dart';

/// Shared input decoration (kept as a function so it does not depend on theme-API churn).
InputDecoration fieldDecoration(String label, {String? hint, Widget? suffixIcon, String? helper}) =>
    InputDecoration(
      labelText: label,
      hintText: hint,
      helperText: helper,
      suffixIcon: suffixIcon,
      border: const OutlineInputBorder(),
    );

/// Inline error with optional retry. Announced to screen readers as a live region.
class ErrorBanner extends StatelessWidget {
  const ErrorBanner({super.key, required this.message, this.onRetry, this.onDismiss});

  final String message;
  final VoidCallback? onRetry;
  final VoidCallback? onDismiss;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Semantics(
      liveRegion: true,
      container: true,
      child: Card(
        color: scheme.errorContainer,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(Icons.error_outline, color: scheme.onErrorContainer),
              const SizedBox(width: 12),
              Expanded(
                child: Text(message, style: TextStyle(color: scheme.onErrorContainer)),
              ),
              if (onRetry != null)
                TextButton(onPressed: onRetry, child: const Text('Retry')),
              if (onDismiss != null)
                IconButton(
                  tooltip: 'Dismiss',
                  onPressed: onDismiss,
                  icon: Icon(Icons.close, color: scheme.onErrorContainer),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class LoadingView extends StatelessWidget {
  const LoadingView({super.key, this.message});

  final String? message;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const CircularProgressIndicator(),
              if (message != null) ...[
                const SizedBox(height: 16),
                Text(message!, textAlign: TextAlign.center),
              ],
            ],
          ),
        ),
      );
}

/// Centered message used for empty / error states.
class MessageView extends StatelessWidget {
  const MessageView({
    super.key,
    required this.icon,
    required this.title,
    this.message,
    this.actionLabel,
    this.onAction,
  });

  final IconData icon;
  final String title;
  final String? message;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 56, color: Theme.of(context).colorScheme.primary),
              const SizedBox(height: 12),
              Text(title, style: Theme.of(context).textTheme.titleLarge, textAlign: TextAlign.center),
              if (message != null) ...[
                const SizedBox(height: 8),
                Text(message!, textAlign: TextAlign.center),
              ],
              if (actionLabel != null && onAction != null) ...[
                const SizedBox(height: 16),
                FilledButton(onPressed: onAction, child: Text(actionLabel!)),
              ],
            ],
          ),
        ),
      );
}

/// Filled button that swaps its label for a spinner while [loading] and blocks double-taps.
class LoadingButton extends StatelessWidget {
  const LoadingButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.loading = false,
    this.icon,
  });

  final String label;
  final VoidCallback? onPressed;
  final bool loading;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final child = loading
        ? const SizedBox(
            height: 20,
            width: 20,
            child: CircularProgressIndicator(strokeWidth: 2),
          )
        : Text(label);
    final callback = loading ? null : onPressed;
    if (icon != null && !loading) {
      return FilledButton.icon(onPressed: callback, icon: Icon(icon), label: child);
    }
    return FilledButton(onPressed: callback, child: child);
  }
}

class SectionCard extends StatelessWidget {
  const SectionCard({super.key, required this.title, required this.child, this.icon});

  final String title;
  final Widget child;
  final IconData? icon;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  if (icon != null) ...[
                    Icon(icon, size: 20),
                    const SizedBox(width: 8),
                  ],
                  Expanded(
                    child: Semantics(
                      header: true,
                      child: Text(title, style: Theme.of(context).textTheme.titleMedium),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              child,
            ],
          ),
        ),
      );
}

class TagWrap extends StatelessWidget {
  const TagWrap({super.key, required this.items, this.emptyText = 'None'});

  final List<String> items;
  final String emptyText;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return Text(emptyText, style: Theme.of(context).textTheme.bodyMedium);
    return Wrap(
      spacing: 8,
      runSpacing: 4,
      children: [for (final item in items) Chip(label: Text(item))],
    );
  }
}

/// Labelled 0-100 meter with a text value (never relies on colour alone).
class ScoreBar extends StatelessWidget {
  const ScoreBar({super.key, required this.label, required this.value, this.color});

  final String label;
  final int value;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final clamped = value.clamp(0, 100);
    return Semantics(
      label: '$label $clamped percent',
      excludeSemantics: true,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [Text(label), Text('$clamped%')],
          ),
          const SizedBox(height: 4),
          LinearProgressIndicator(value: clamped / 100, color: color, minHeight: 8),
        ],
      ),
    );
  }
}

/// Transparency notice shown wherever AI output is displayed.
class AiDisclosureBanner extends StatelessWidget {
  const AiDisclosureBanner({
    super.key,
    this.text = 'AI-generated guidance. It can be wrong or out of date - treat it as a starting '
        'point and confirm important decisions with a qualified counsellor.',
  });

  final String text;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Semantics(
      container: true,
      label: 'AI disclosure. $text',
      excludeSemantics: true,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: scheme.secondaryContainer,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(Icons.smart_toy_outlined, size: 20, color: scheme.onSecondaryContainer),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  text,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(color: scheme.onSecondaryContainer),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Status chip with icon + text (colour is supplementary).
class StatusChip extends StatelessWidget {
  const StatusChip({super.key, required this.status});

  final String status;

  static Color colorFor(String status) {
    switch (status) {
      case 'Approved':
      case 'approved':
        return Colors.green.shade700;
      case 'Rejected':
      case 'rejected':
        return Colors.red.shade700;
      case 'NeedsRevision':
        return Colors.orange.shade800;
      default:
        return Colors.blue.shade700;
    }
  }

  static IconData iconFor(String status) {
    switch (status) {
      case 'Approved':
      case 'approved':
        return Icons.check_circle_outline;
      case 'Rejected':
      case 'rejected':
        return Icons.cancel_outlined;
      case 'NeedsRevision':
        return Icons.edit_note;
      default:
        return Icons.hourglass_top;
    }
  }

  static String labelFor(String status) => status == 'NeedsRevision' ? 'Revision requested' : status;

  @override
  Widget build(BuildContext context) {
    final color = colorFor(status);
    return Chip(
      avatar: Icon(iconFor(status), size: 18, color: color),
      label: Text(labelFor(status)),
      side: BorderSide(color: color),
    );
  }
}
