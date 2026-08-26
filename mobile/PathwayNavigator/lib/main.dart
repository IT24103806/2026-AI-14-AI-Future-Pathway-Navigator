import 'package:flutter/material.dart';

import 'career_discovery_screen.dart';

void main() {
  runApp(const MainApp());
}

class MainApp extends StatelessWidget {
  const MainApp({super.key});

  @override
  Widget build(BuildContext context) {
    return const MaterialApp(
      title: 'PathwayNavigator',
      home: CareerDiscoveryScreen(),
    );
  }
}
