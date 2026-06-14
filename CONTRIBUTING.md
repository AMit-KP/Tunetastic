# Contributing to Tunetastic

Thanks for taking the time to contribute! Whether it's a bug fix, a new feature, or just improving the docs — all contributions are welcome.

Please read through this before opening an issue or submitting a pull request.

---

## Before You Start

- **Check existing issues** first to see if your bug or idea is already being tracked.
- For anything beyond a small fix, **open an issue to discuss it first**. This saves you from putting work into something that might not be a good fit for the project.

---

## Reporting Bugs

When filing a bug report, please include:
- Your Windows version (e.g. Windows 11 24H2, Windows 10 22H2)
- Where you installed from (Microsoft Store or GitHub release)
- Steps to reproduce the issue
- What you expected to happen vs. what actually happened
- Any relevant screenshots or error messages

---

## Suggesting Features

Open a feature request issue and describe:
- What you want the feature to do
- Why it would be useful (your use case)
- Any examples from other apps if relevant

---

## Submitting a Pull Request

1. Fork the repository and create a new branch from `Development`
2. Make your changes, keeping them focused — one fix or feature per PR
3. Make sure the project builds cleanly in Visual Studio 2026 before submitting
4. Write a clear PR description explaining what changed and why
5. **Open your pull request against the `Development` branch** — this is the only branch contributors should target. Merges from `Development` into `master` are handled separately by the maintainer as part of releases.

### Code Style

- Follow the existing code style and naming conventions in the project
- Keep WinUI/XAML markup clean and consistent with the surrounding files
- Avoid introducing unnecessary dependencies

---

## Setting Up the Project

```bash
git clone -b Development https://github.com/AMit-KP/Tunetastic.git
cd Tunetastic
start Tunetastic.sln
```

Required Visual Studio 2026 workloads:
- **.NET desktop development**
- **Windows application development** (includes Windows App SDK)

Press **F5** to build and run.

---

## License

By contributing, you agree that your contributions will be licensed under the [GNU General Public License v3.0](LICENSE).
