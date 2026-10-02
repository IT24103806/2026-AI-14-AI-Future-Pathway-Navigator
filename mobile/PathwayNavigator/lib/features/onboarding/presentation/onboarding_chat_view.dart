import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/widgets/common_widgets.dart';
import '../data/onboarding_models.dart';
import 'onboarding_chat_controller.dart';

const List<String> _suggestions = [
  'I am an Undergraduate student',
  'I just completed my A/Ls',
  'I know Python and Problem Solving',
  'I am interested in Robotics and AI',
  'I want to become an AI Engineer',
];

class OnboardingChatView extends StatefulWidget {
  const OnboardingChatView({super.key});

  @override
  State<OnboardingChatView> createState() => _OnboardingChatViewState();
}

class _OnboardingChatViewState extends State<OnboardingChatView> {
  final _input = TextEditingController();
  final _scroll = ScrollController();

  @override
  void dispose() {
    _input.dispose();
    _scroll.dispose();
    super.dispose();
  }

  void _send(OnboardingChatController controller, String text) {
    if (text.trim().isEmpty) return;
    _input.clear();
    controller.send(text);
    _scrollToEndSoon();
  }

  void _scrollToEndSoon() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!_scroll.hasClients) return;
      _scroll.animateTo(
        _scroll.position.maxScrollExtent,
        duration: const Duration(milliseconds: 250),
        curve: Curves.easeOut,
      );
    });
  }

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<OnboardingChatController>();
    _scrollToEndSoon();

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
          child: Column(
            children: [
              const AiDisclosureBanner(
                text: 'You are chatting with an AI assistant. Only share what you are comfortable '
                    'storing in your profile - never passwords or ID numbers.',
              ),
              const SizedBox(height: 8),
              _SlotProgress(slots: controller.slots),
            ],
          ),
        ),
        Expanded(
          child: ListView.builder(
            controller: _scroll,
            padding: const EdgeInsets.all(16),
            itemCount: controller.messages.length + (controller.isTyping ? 1 : 0),
            itemBuilder: (context, index) {
              if (index >= controller.messages.length) return const _TypingBubble();
              return _MessageBubble(message: controller.messages[index]);
            },
          ),
        ),
        if (controller.error != null)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: ErrorBanner(message: controller.error!, onDismiss: controller.clearError),
          ),
        if (controller.isComplete)
          Padding(
            padding: const EdgeInsets.all(16),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    const Text('Your profile is saved. You are ready to discover career pathways!'),
                    const SizedBox(height: 12),
                    FilledButton(onPressed: () => context.go(AppRoutes.student), child: const Text('Go to dashboard')),
                  ],
                ),
              ),
            ),
          )
        else ...[
          SizedBox(
            height: 48,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16),
              itemCount: _suggestions.length,
              separatorBuilder: (_, _) => const SizedBox(width: 8),
              itemBuilder: (_, index) => ActionChip(
                label: Text(_suggestions[index]),
                onPressed: controller.isTyping ? null : () => _send(controller, _suggestions[index]),
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _input,
                    enabled: !controller.isTyping,
                    minLines: 1,
                    maxLines: 4,
                    textInputAction: TextInputAction.send,
                    onSubmitted: (text) => _send(controller, text),
                    decoration: fieldDecoration('Message', hint: 'Type your answer…'),
                  ),
                ),
                const SizedBox(width: 8),
                IconButton.filled(
                  tooltip: 'Send message',
                  onPressed: controller.isTyping ? null : () => _send(controller, _input.text),
                  icon: const Icon(Icons.send),
                ),
              ],
            ),
          ),
        ],
      ],
    );
  }
}

class _SlotProgress extends StatelessWidget {
  const _SlotProgress({required this.slots});

  final ProfileSlots slots;

  @override
  Widget build(BuildContext context) {
    final filled = slots.filledCount;
    return Semantics(
      label: 'Profile progress: $filled of ${ProfileSlots.totalSlots} details collected',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Profile details collected: $filled / ${ProfileSlots.totalSlots}',
              style: Theme.of(context).textTheme.labelMedium),
          const SizedBox(height: 4),
          LinearProgressIndicator(value: filled / ProfileSlots.totalSlots),
        ],
      ),
    );
  }
}

class _MessageBubble extends StatelessWidget {
  const _MessageBubble({required this.message});

  final ChatMessage message;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final isUser = message.role == ChatRole.user;
    return Align(
      alignment: isUser ? Alignment.centerRight : Alignment.centerLeft,
      child: Semantics(
        label: '${isUser ? 'You' : 'Pathway Guide'}: ${message.content}',
        excludeSemantics: true,
        child: Container(
          margin: const EdgeInsets.symmetric(vertical: 4),
          padding: const EdgeInsets.all(12),
          constraints: BoxConstraints(maxWidth: MediaQuery.sizeOf(context).width * 0.8),
          decoration: BoxDecoration(
            color: isUser ? scheme.primaryContainer : scheme.surfaceContainerHighest,
            borderRadius: BorderRadius.circular(16),
          ),
          child: Text(message.content),
        ),
      ),
    );
  }
}

class _TypingBubble extends StatelessWidget {
  const _TypingBubble();

  @override
  Widget build(BuildContext context) => const Align(
        alignment: Alignment.centerLeft,
        child: Padding(
          padding: EdgeInsets.symmetric(vertical: 8),
          child: Text('Pathway Guide is typing…'),
        ),
      );
}
