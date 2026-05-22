module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    // Padrão Conventional Commits: 100. Aumentado para mensagens em português no corpo.
    'header-max-length': [2, 'always', 120],
    'body-max-line-length': [2, 'always', 500],
  },
};
