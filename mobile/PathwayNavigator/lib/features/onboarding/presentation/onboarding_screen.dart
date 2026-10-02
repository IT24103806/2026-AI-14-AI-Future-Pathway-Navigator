import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../data/onboarding_repository.dart';
import 'onboarding_chat_controller.dart';
import 'onboarding_chat_view.dart';
import 'onboarding_form_view.dart';

enum _OnboardingMode { chat, form }

class OnboardingScreen extends StatefulWidget {
  const OnboardingScreen({super.key});

  @override
  State<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends State<OnboardingScreen> {
  _OnboardingMode _mode = _OnboardingMode.chat;

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<OnboardingChatController>(
      create: (context) => OnboardingChatController(context.read<OnboardingRepository>()),
      child: Scaffold(
        appBar: AppBar(title: const Text('Set up your profile')),
        body: Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(12),
              child: SegmentedButton<_OnboardingMode>(
                segments: const [
                  ButtonSegment(value: _OnboardingMode.chat, label: Text('AI guide'), icon: Icon(Icons.smart_toy_outlined)),
                  ButtonSegment(value: _OnboardingMode.form, label: Text('Form'), icon: Icon(Icons.edit_note)),
                ],
                selected: {_mode},
                onSelectionChanged: (selection) => setState(() => _mode = selection.first),
              ),
            ),
            Expanded(
              child: _mode == _OnboardingMode.chat ? const OnboardingChatView() : const OnboardingFormView(),
            ),
          ],
        ),
      ),
    );
  }
}
