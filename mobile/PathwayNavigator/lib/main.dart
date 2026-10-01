import 'package:flutter/material.dart';

import 'app/app.dart';
import 'app/dependencies.dart';
import 'core/config/app_config.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  final AppConfig config;
  try {
    config = AppConfig.fromEnvironment();
  } on StateError catch (error) {
    // Misconfigured build (e.g. HTTP in release): show the problem instead of a blank screen.
    runApp(_ConfigErrorApp(message: error.message));
    return;
  }

  runApp(PathwayNavigatorApp(dependencies: AppDependencies.create(config: config)));
}

class _ConfigErrorApp extends StatelessWidget {
  const _ConfigErrorApp({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) => MaterialApp(
        home: Scaffold(
          body: SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Center(child: Text('Configuration error\n\n$message', textAlign: TextAlign.center)),
            ),
          ),
        ),
      );
}
