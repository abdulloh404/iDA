import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../auth/new_password_screen.dart';
import '../auth/set_pin_screen.dart';
import '../auth/terms_screen.dart';
import '../services/menu.dart';
import '../shell/hospital_switcher.dart';


class AccountScreen extends StatelessWidget {
  const AccountScreen({super.key});

  Future<void> _signOut(BuildContext context) async {
    final s = S.of(context);
    final ok = await confirmDialog(
      context,
      title: s.signOutTitle,
      body: s.signOutBody,
      confirmLabel: s.signOut,
      destructive: true,
      icon: Icons.logout_rounded,
    );
    if (ok && context.mounted) await AppScope.read(context).signOut();
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final app = AppScope.of(context);
    final session = app.requireSession;
    final en = app.isEnglish;
    final daysLeft = session.passwordExpiresAt.difference(DateTime.now()).inDays;
    final expiring = daysLeft <= 14;

    return Scaffold(
      appBar: AppBar(title: Text(s.accountTitle), automaticallyImplyLeading: false),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s10),
        children: [
          ContentWidth(
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              IdaCard(
                child: Column(children: [
                  Row(children: [
                    IdaAvatar(session.user.initials, size: IdaSizes.avatarLg),
                    const SizedBox(width: IdaSpace.s4),
                    Expanded(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(session.user.displayName(en), style: context.text.titleMedium),
                        Text(session.user.position(en), style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
                        const SizedBox(height: IdaSpace.s1),
                        Text(session.user.username, style: idaNumeric(context.text.bodySmall!)),
                      ]),
                    ),
                  ]),
                  const SizedBox(height: IdaSpace.s4),
                  Divider(color: p.border),
                  _Row(
                    icon: Icons.local_hospital_outlined,
                    title: session.hospital.name(en),
                    subtitle: [
                      session.hospital.roleName(en),
                      if (session.hospital.doctorCode != null) session.hospital.doctorCode!,
                    ].join(' · '),
                    onTap: session.hospitals.length > 1 ? () => showHospitalSwitcher(context) : null,
                    trailing: session.hospitals.length > 1
                        ? Tooltip(
                            message: s.selectHospital,
                            child: Icon(Icons.swap_horiz_rounded, color: p.primaryText),
                          )
                        : null,
                  ),
                ]),
              ),
              if (expiring) ...[
                const SizedBox(height: IdaSpace.s3),
                Container(
                  padding: const EdgeInsets.all(IdaSpace.s4),
                  decoration: BoxDecoration(color: p.warningSoft, borderRadius: IdaRadius.lgR),
                  child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Icon(Icons.warning_amber_rounded, color: p.warningText),
                    const SizedBox(width: IdaSpace.s3),
                    Expanded(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(s.passwordExpiringTitle, style: context.text.titleSmall!.copyWith(color: p.warningText)),
                        Text(s.passwordExpiringBody(daysLeft), style: context.text.bodySmall!.copyWith(color: p.warningText)),
                      ]),
                    ),
                  ]),
                ),
              ],
              if (session.isDoctor) ...[
                const SizedBox(height: IdaSpace.s5),
                SectionHeader(s.groupMyInfo),
                _Card(children: [
                  _Row(icon: AppMenu.profile.icon, title: s.menuProfile, onTap: () => AppMenu.profile.open(context)),
                  _Row(icon: AppMenu.bank.icon, title: s.menuBank, onTap: () => AppMenu.bank.open(context)),
                ]),
              ],
              const SizedBox(height: IdaSpace.s5),
              SectionHeader(s.security),
              _Card(children: [
                _Row(
                  icon: Icons.password_rounded,
                  title: s.changePassword,
                  subtitle: s.passwordExpiresIn(daysLeft),
                  subtitleColor: expiring ? p.warningText : null,
                  onTap: () => pushPage<void>(context, const NewPasswordScreen(mode: PasswordMode.change)),
                ),
                _Row(
                  icon: Icons.pin_outlined,
                  title: s.changePin,
                  onTap: () => pushPage<void>(context, const SetPinScreen(mode: PinMode.change)),
                ),
              ]),
              const SizedBox(height: IdaSpace.s5),
              SectionHeader(s.display),
              _Card(children: [
                Padding(
                  padding: const EdgeInsets.all(IdaSpace.s4),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    Row(children: [
                      IconBox(Icons.translate_rounded, size: IdaSizes.avatarSm),
                      const SizedBox(width: IdaSpace.s3),
                      Text(s.language, style: context.text.titleSmall),
                    ]),
                    const SizedBox(height: IdaSpace.s3),
                    IdaSegmented<String>(
                      key: const Key('lang-seg'),
                      options: const {'th': 'ไทย', 'en': 'English'},
                      value: app.locale.languageCode,
                      onChanged: (v) => app.setLocale(Locale(v)),
                    ),
                    const SizedBox(height: IdaSpace.s5),
                    Row(children: [
                      IconBox(Icons.contrast_rounded, size: IdaSizes.avatarSm),
                      const SizedBox(width: IdaSpace.s3),
                      Text(s.theme, style: context.text.titleSmall),
                    ]),
                    const SizedBox(height: IdaSpace.s3),
                    IdaSegmented<ThemeMode>(
                      options: {
                        ThemeMode.system: s.themeSystem,
                        ThemeMode.light: s.themeLight,
                        ThemeMode.dark: s.themeDark,
                      },
                      value: app.themeMode,
                      onChanged: app.setThemeMode,
                    ),
                  ]),
                ),
              ]),
              const SizedBox(height: IdaSpace.s5),
              SectionHeader(s.about),
              _Card(children: [
                _Row(
                  icon: Icons.gavel_rounded,
                  title: s.termsTitle,
                  onTap: () => pushPage<void>(context, const TermsScreen()),
                ),
                _Row(
                  icon: Icons.info_outline_rounded,
                  title: s.version,
                  subtitle: '1.0.0 (1)${app.isDemo ? ' · ${s.demoMode}' : ''}',
                ),
              ]),
              const SizedBox(height: IdaSpace.s6),
              OutlinedButton.icon(
                key: const Key('sign-out'),
                style: OutlinedButton.styleFrom(
                  foregroundColor: p.dangerText,
                  side: BorderSide(color: p.dangerText, width: 1.5),
                ),
                onPressed: () => _signOut(context),
                icon: const Icon(Icons.logout_rounded, size: IdaSizes.iconMd),
                label: Text(s.signOut),
              ),
              const SizedBox(height: IdaSpace.s6),
              Center(child: IdaLogo.mark(height: IdaSizes.iconXl, reversed: context.isDark)),
            ]),
          ),
        ],
      ),
    );
  }
}

class _Card extends StatelessWidget {
  const _Card({required this.children});
  final List<Widget> children;

  @override
  Widget build(BuildContext context) => IdaCard(
        padding: EdgeInsets.zero,
        child: Column(children: [
          for (final (i, c) in children.indexed) ...[
            if (i > 0) Divider(indent: IdaSpace.s16, color: context.ida.border),
            c,
          ],
        ]),
      );
}

class _Row extends StatelessWidget {
  const _Row({required this.icon, required this.title, this.subtitle, this.subtitleColor, this.onTap, this.trailing});

  final IconData icon;
  final String title;
  final String? subtitle;
  final Color? subtitleColor;
  final VoidCallback? onTap;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return InkWell(
      onTap: onTap,
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: IdaSizes.controlHeightLg + IdaSpace.s3),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4, vertical: IdaSpace.s2),
          child: Row(children: [
            IconBox(icon, size: IdaSizes.avatarSm),
            const SizedBox(width: IdaSpace.s3),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisSize: MainAxisSize.min, children: [
                Text(title, style: context.text.bodyLarge!.copyWith(fontWeight: FontWeight.w500)),
                if (subtitle != null)
                  Text(subtitle!, style: context.text.bodySmall!.copyWith(color: subtitleColor)),
              ]),
            ),
            if (trailing != null) trailing! else if (onTap != null) Icon(Icons.chevron_right_rounded, color: p.borderStrong),
          ]),
        ),
      ),
    );
  }
}
