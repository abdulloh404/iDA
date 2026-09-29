import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import 'new_password_screen.dart';
import 'set_pin_screen.dart';
import 'terms_screen.dart';


class LanguageToggle extends StatelessWidget {
  const LanguageToggle({super.key, this.onBrand = false});
  final bool onBrand;

  @override
  Widget build(BuildContext context) {
    final app = AppScope.of(context);
    final s = S.of(context);
    final p = context.ida;
    final en = app.isEnglish;
    final fg = onBrand ? IdaColors.textInverse : p.text;
    return Semantics(
      button: true,
      label: s.langToggleLabel,
      excludeSemantics: true,
      child: Material(
        color: onBrand ? IdaColors.textInverse.withValues(alpha: 0.16) : p.surface,
        shape: StadiumBorder(
          side: BorderSide(color: onBrand ? IdaColors.textInverse.withValues(alpha: 0.4) : p.border),
        ),
        child: InkWell(
          customBorder: const StadiumBorder(),
          onTap: app.toggleLocale,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: IdaSizes.minTapTarget, minWidth: IdaSizes.minTapTarget),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s3),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                ClipRRect(
                  borderRadius: IdaRadius.smR,
                  child: Image.asset(en ? 'assets/icons/flag-en.png' : 'assets/icons/flag-th.png',
                      width: IdaSizes.iconMd, height: IdaSizes.iconSm, fit: BoxFit.cover),
                ),
                const SizedBox(width: IdaSpace.s2),
                Text(en ? 'EN' : 'TH', style: context.text.labelLarge!.copyWith(color: fg)),
                Icon(Icons.swap_horiz_rounded, size: IdaSizes.iconSm, color: fg),
              ]),
            ),
          ),
        ),
      ),
    );
  }
}


class AuthHeader extends StatelessWidget {
  const AuthHeader({super.key, required this.icon, required this.title, this.subtitle, this.step});

  final IconData icon;
  final String title;
  final String? subtitle;
  final String? step;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: IdaSizes.emptyCircle,
          height: IdaSizes.emptyCircle,
          decoration: const BoxDecoration(shape: BoxShape.circle, gradient: IdaColors.gradientCardAccent),
          child: Icon(icon, color: IdaColors.textInverse, size: IdaSizes.iconXl),
        ),
        const SizedBox(height: IdaSpace.s5),
        if (step != null) ...[
          Text(step!, style: context.text.labelMedium!.copyWith(color: p.accentStrong)),
          const SizedBox(height: IdaSpace.s1),
        ],
        Semantics(header: true, child: Text(title, style: context.text.headlineSmall)),
        const SizedBox(height: IdaSpace.s2),
        const IdaBrandTick(),
        if (subtitle != null) ...[
          const SizedBox(height: IdaSpace.s3),
          Text(subtitle!, style: context.text.bodyLarge!.copyWith(color: p.textSecondary)),
        ],
      ],
    );
  }
}


class OnboardingFlow {
  OnboardingFlow(this.result);

  final SignInResult result;
  int _index = -1;

  late final List<Widget Function()> _steps = [
    if (result.mustAcceptTerms) () => TermsScreen(flow: this),
    if (result.mustChangePassword) () => NewPasswordScreen(mode: PasswordMode.firstLogin, flow: this),
    if (result.needsPin) () => SetPinScreen(mode: PinMode.setup, flow: this),
  ];

  int get total => _steps.length;
  int get current => _index + 1;

  String? stepLabel(S s) => total > 1 ? s.stepOf(current, total) : null;

  void start(BuildContext context) => advance(context);

  void advance(BuildContext context) {
    _index++;
    if (_index >= _steps.length) {
      AppScope.read(context).enter(result.session);
      return;
    }


    final page = _steps[_index]();
    Navigator.of(context).pushReplacement(MaterialPageRoute(builder: (_) => page));
  }


  Future<void> abort(BuildContext context) => AppScope.read(context).signOut();
}
