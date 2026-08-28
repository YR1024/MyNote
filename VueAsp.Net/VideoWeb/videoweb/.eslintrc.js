module.exports = {
  root: true,
  env: {
    node: true,
  },
  extends: ["eslint:recommended", "plugin:vue/vue3-recommended"],
  parserOptions: {
    ecmaVersion: 2020,
  },
  rules: {
    "vue/multi-word-component-names": "off",
    "vue/html-closing-bracket-spacing": "off",
    "vue/order-in-components": "off",

    "vue/max-attributes-per-line": "off",
    "vue/html-self-closing": "off",
    "vue/attributes-order": "off",
    "vue/singleline-html-element-content-newline": "off",
  },
};
